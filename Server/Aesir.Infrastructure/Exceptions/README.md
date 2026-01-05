# Database Constraint Exception Handling

This directory contains a reusable exception infrastructure for handling database constraint violations with user-friendly error messages.

## Overview

When database operations violate constraints (foreign keys, unique indexes, check constraints), PostgreSQL returns cryptic error codes. This infrastructure detects these violations and throws typed exceptions with friendly, actionable error messages.

## Exception Hierarchy

```
DatabaseConstraintException (abstract base)
├── ForeignKeyViolationException (foreign key constraint violations)
├── UniqueConstraintViolationException (future: unique constraint violations)
└── CheckConstraintViolationException (future: check constraint violations)
```

## How It Works

### 1. Service Layer Detection

The service layer checks for constraint violations **before** attempting database operations:

```csharp
// Step 1: Get entity for friendly error messages
var agent = await GetAgentAsync(id);
if (agent == null)
{
    throw new EntityNotFoundException("Agent", id);
}

// Step 2: Check for foreign key references
const string checkSql = @"
    SELECT COUNT(*) FROM aesir.aesir_research_team_member WHERE agent_id = @Id
";

var refCount = await dbContext.UnitOfWorkAsync(async connection =>
    await connection.QueryFirstAsync<int>(checkSql, new { Id = id }));

// Step 3: Throw friendly exception if references exist
if (refCount > 0)
{
    throw new ForeignKeyViolationException(
        constraintName: "FK_aesir_research_team_member_agent_id_aesir_agent_id",
        entityName: agent.Name,
        message: $"Cannot delete agent '{agent.Name}' because it is assigned to {refCount} research team(s).",
        suggestedAction: "Remove the agent from all research teams before deleting.");
}

// Step 4: Safe to proceed with deletion
```

### 2. Controller Layer Handling

The controller layer catches typed exceptions and returns appropriate HTTP status codes:

```csharp
try
{
    await configurationService.DeleteAgentAsync(id);
    return NoContent(); // 204
}
catch (ForeignKeyViolationException ex)
{
    logger.LogWarning(ex, "Cannot delete agent {Id}: {Constraint}", id, ex.ConstraintName);

    return Conflict(new  // 409 Conflict
    {
        error = "ConstraintViolation",
        message = ex.Message,
        suggested_action = ex.SuggestedAction,
        constraint_name = ex.ConstraintName,
        entity_name = ex.EntityName
    });
}
catch (EntityNotFoundException ex)
{
    return NotFound(new { error = "NotFound", message = ex.Message }); // 404
}
catch (Exception ex)
{
    logger.LogError(ex, "Unexpected error deleting agent {Id}", id);
    return StatusCode(500, "An unexpected error occurred"); // 500
}
```

## HTTP Status Codes

| Status Code | Exception Type | Use Case |
|-------------|----------------|----------|
| 204 No Content | N/A | Successful deletion |
| 404 Not Found | EntityNotFoundException | Entity doesn't exist |
| 409 Conflict | ForeignKeyViolationException | Resource is referenced by other entities |
| 500 Internal Server Error | Exception (generic) | Unexpected errors |

## Error Response Format

All error responses use `snake_case` JSON (consistent with client serialization):

```json
{
  "error": "ConstraintViolation",
  "message": "Cannot delete agent 'Research Assistant' because it is assigned to 3 research team(s).",
  "suggested_action": "Remove the agent from all research teams before deleting.",
  "constraint_name": "FK_aesir_research_team_member_agent_id_aesir_agent_id",
  "entity_name": "Research Assistant"
}
```

## Adopting This Pattern in Your Module

### Step 1: Add Using Statement

```csharp
using Aesir.Infrastructure.Exceptions;
```

### Step 2: Update Service Delete Method

```csharp
public async Task DeleteMyEntityAsync(Guid id)
{
    VerifyIsDatabaseMode();

    // 1. Get entity for friendly messages
    var entity = await GetMyEntityAsync(id);
    if (entity == null)
    {
        throw new EntityNotFoundException("MyEntity", id);
    }

    // 2. Check for foreign key references
    const string checkSql = @"
        SELECT COUNT(*) FROM aesir.aesir_referencing_table
        WHERE my_entity_id = @Id
    ";

    var refCount = await dbContext.UnitOfWorkAsync(async connection =>
        await connection.QueryFirstAsync<int>(checkSql, new { Id = id }));

    // 3. Throw friendly exception if references exist
    if (refCount > 0)
    {
        throw new ForeignKeyViolationException(
            constraintName: "FK_aesir_referencing_table_my_entity_id_aesir_my_entity_id",
            entityName: entity.Name,
            message: $"Cannot delete {entity.Name} because it is referenced by {refCount} item(s).",
            suggestedAction: "Remove all references before deleting.");
    }

    // 4. Proceed with deletion
    const string deleteSql = @"
        DELETE FROM aesir.aesir_my_entity WHERE id = @Id::uuid
    ";

    await dbContext.UnitOfWorkAsync(async connection =>
        await connection.ExecuteAsync(deleteSql, new { Id = id }));
}
```

### Step 3: Update Controller Delete Endpoint

```csharp
[HttpDelete("myentities/{id:guid}")]
public async Task<IActionResult> DeleteMyEntityAsync([FromRoute] Guid id)
{
    try
    {
        await myService.DeleteMyEntityAsync(id);
        return NoContent();
    }
    catch (ForeignKeyViolationException ex)
    {
        logger.LogWarning(ex, "Cannot delete entity {Id}: {Constraint}",
            id, ex.ConstraintName);

        return Conflict(new
        {
            error = "ConstraintViolation",
            message = ex.Message,
            suggested_action = ex.SuggestedAction,
            constraint_name = ex.ConstraintName,
            entity_name = ex.EntityName
        });
    }
    catch (EntityNotFoundException ex)
    {
        logger.LogWarning("Entity not found: {Id}", id);
        return NotFound(new { error = "NotFound", message = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error deleting entity {Id}", id);
        return StatusCode(500, "An unexpected error occurred");
    }
}
```

### Step 4: Add Unit Tests

```csharp
[Fact]
public async Task DeleteMyEntity_WhenReferenced_ThrowsForeignKeyViolationException()
{
    // Arrange
    var entityId = Guid.NewGuid();
    // ... setup mocks with references

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ForeignKeyViolationException>(
        () => service.DeleteMyEntityAsync(entityId));

    Assert.Contains("referenced", exception.Message);
    Assert.NotNull(exception.SuggestedAction);
}

[Fact]
public async Task DeleteMyEntity_WhenNoReferences_SuccessfullyDeletes()
{
    // Arrange
    var entityId = Guid.NewGuid();
    // ... setup mocks with no references

    // Act
    await service.DeleteMyEntityAsync(entityId);

    // Assert
    // ... verify deletion occurred
}
```

## Writing Friendly Error Messages

### Best Practices

1. **Include the entity name**: `"Cannot delete agent 'Research Assistant'..."`
2. **State why deletion failed**: `"...because it is assigned to 3 research team(s)"`
3. **Provide actionable guidance**: `"Remove the agent from all research teams before deleting."`
4. **Use counts when relevant**: `"...used by 5 agent(s)"`
5. **Distinguish historical data**: `"Research submissions contain historical data that must be preserved. Consider deactivating the agent instead."`

### Message Templates

**For removable references:**
```
Cannot delete {entity name} because it is {relationship} {count} {related entity}(s).
Suggested Action: Remove the {entity name} from all {related entities} before deleting.
```

**For historical data:**
```
Cannot delete {entity name} because it has {action} {count} {related entity}(s).
Suggested Action: {Historical data reason}. Consider deactivating the {entity name} instead.
```

**For configuration references:**
```
Cannot delete {entity name} because it is configured as the {configuration setting}.
Suggested Action: Choose a different {configuration setting} before deleting.
```

## Testing Requirements

Every delete method with FK validation **must** have:

1. ✅ Test for FK violation (should throw ForeignKeyViolationException)
2. ✅ Test for entity not found (should throw EntityNotFoundException)
3. ✅ Test for successful deletion (no references, should succeed)
4. ✅ Controller test for 409 Conflict response
5. ✅ Controller test for 404 Not Found response
6. ✅ Controller test for 204 No Content response

## Future Extensions

This infrastructure can be extended to handle:

- **Unique constraint violations** (SqlState: `23505`)
  - Use case: "Username already exists"
  - Return 409 Conflict

- **Check constraint violations** (SqlState: `23514`)
  - Use case: "Invalid value range"
  - Return 400 Bad Request

- **Not-null violations** (SqlState: `23502`)
  - Use case: "Required field missing"
  - Return 400 Bad Request

## Example Implementation

See `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Configuration/` for a complete reference implementation:

- **Service**: `Services/ConfigurationService.cs`
  - `DeleteAgentAsync` (lines 614-675)
  - `DeleteInferenceEngineAsync` (lines 455-514)

- **Controller**: `Controllers/ConfigurationController.cs`
  - `DeleteAgentAsync` endpoint (lines 352-387)
  - `DeleteInferenceEngineAsync` endpoint (lines 205-240)

- **Tests**: `../Aesir.Modules.Configuration.Tests/Services/ConfigurationServiceTests.cs`

## Questions or Issues?

If you encounter issues adopting this pattern or have questions about constraint handling, please:

1. Review the reference implementation in Configuration module
2. Check ISSUES.md for known constraint-related issues
3. Consult with the team lead or create an issue

---

**Last Updated**: 2026-01-05
**Version**: 1.0.0
**Status**: Production Ready
