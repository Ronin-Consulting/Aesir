# Known Issues

<!--
TEMPLATE INSTRUCTIONS:
1. Use this file to track known issues, bugs, and workarounds
2. Each issue should have a unique sequential number (#1, #2, etc.)
3. Update status as issues are investigated and resolved
4. Keep resolved issues for historical reference (don't delete)
5. Severity levels: Critical, High, Medium, Low
-->

---

## Issue #X: [Brief descriptive title]

<!--
COPY THIS TEMPLATE FOR EACH NEW ISSUE:
Replace X with the next issue number and fill in all sections.
-->

**Date:** YYYY-MM-DD

**Severity:** [Critical | High | Medium | Low]
- Critical: System unusable, data loss risk
- High: Major feature broken, no workaround
- Medium: Feature impaired but workaround exists
- Low: Minor issue, cosmetic, or edge case

**Test:** `[TestClassName.TestMethodName]` (if discovered via test)

**Query/Input:** "[User input or action that triggers the issue]"

**Symptom:**
[Description of what the user sees or experiences. Include error messages verbatim.]

```
[Paste exact error message or stack trace here]
```

**Expected Behavior:**
[What should happen instead]

**Root Cause:**
[Technical explanation of why this happens. Include relevant code paths or data issues.]

**Relevant Code/Data:**
- Affected file: `Path/To/File.cs:LineNumber`
- Affected table/column: `TableName.ColumnName`
- Actual values: [List actual values vs expected]

**Impact:**
- [How this affects users]
- [Performance implications if any]
- [Data integrity concerns if any]

**Workaround:**
[Steps users or developers can take to avoid the issue. "None needed" if system self-recovers.]

**Proposed Fix:**
[Technical description of the fix. Include code snippets if helpful.]

```csharp
// Example fix code
public void FixedMethod()
{
    // Updated implementation
}
```

**Status:** [OPEN | IN PROGRESS | RESOLVED | NOT A BUG | WONT FIX]

**Resolution:** (fill in when resolved)
- [Summary of what was changed]
- [Files modified]
- [Release number where fix was included]

---

<!--
========================================
EXAMPLE ISSUES BELOW - DELETE OR REPLACE
========================================
-->

## Issue #1: Example - Database column name mismatch

**Date:** 2024-12-01

**Severity:** Medium (query fails but system recovers)

**Test:** `TestMatrix_Geo_SalesByCountry`

**Query/Input:** "Total sales by country"

**Symptom:**
Query fails with column name error on first attempt:
```
Microsoft.Data.SqlClient.SqlException: Invalid column name 'CountryName'.
```

**Expected Behavior:**
Query should succeed on first attempt using the correct column name.

**Root Cause:**
The schema documentation incorrectly listed `CountryName` as the country column. The actual column in the database is `RegionCountryName`.

**Relevant Code/Data:**
- Schema doc: `SamurAICouncil.Core/Resources/Schema.md`
- Actual column: `DimGeography.RegionCountryName`
- Incorrect column: `CountryName` (doesn't exist)

**Impact:**
- First query attempt fails
- System retries and eventually succeeds (self-healing)
- Increased latency and API costs due to retry

**Workaround:**
None needed - system self-corrects via retry mechanism.

**Proposed Fix:**
Update schema documentation to use correct column name:

```markdown
### DimGeography
| Column | Type | Description |
|--------|------|-------------|
| RegionCountryName | nvarchar(100) | Country name (NOT CountryName) |
```

**Status:** RESOLVED (Release 8)

**Resolution:**
- Fixed schema documentation in `Resources/Schema.md`
- Added explicit warning about incorrect column names
- Updated example queries to use correct column

---

## Issue #2: Example - Empty result set (data-dependent)

**Date:** 2024-12-01

**Severity:** Low (may not be a bug)

**Query/Input:** "Which products have low inventory?"

**Symptom:**
Query returns 0 rows. User expects to see products below safety stock.

**Expected Behavior:**
If products exist below safety stock, they should be returned.

**Root Cause:**
The SQL logic is correct (`OnHandQuantity < SafetyStockQuantity`). The 0 rows returned simply means no products in the current dataset are below their safety stock level.

**Relevant Code/Data:**
- Query logic: `WHERE OnHandQuantity < SafetyStockQuantity`
- This is correct business logic

**Impact:**
- None - this is expected behavior for the current data

**Workaround:**
N/A

**Proposed Fix:**
No fix needed. Could optionally add messaging to UI: "No products currently below safety stock levels."

**Status:** NOT A BUG (data-dependent)

---

## Issue #3: Example - Performance degradation

**Date:** 2024-12-01

**Severity:** High

**Symptom:**
Page load times increased from ~500ms to ~3s after recent deployment.

**Expected Behavior:**
Page should load in under 1 second.

**Root Cause:**
Missing database index on `messages.conversation_id` causing full table scan.

**Relevant Code/Data:**
- Affected query: `SELECT * FROM messages WHERE conversation_id = @id ORDER BY created_at`
- Table size: ~50,000 rows
- Missing index on `conversation_id`

**Impact:**
- Degraded user experience
- Increased database CPU usage
- Potential timeout on large conversations

**Workaround:**
None available.

**Proposed Fix:**
Add index via migration:

```csharp
public class AddMessagesConversationIdIndex : Migration
{
    public override void Up()
    {
        Create.Index("IX_messages_conversation_id")
            .OnTable("messages")
            .OnColumn("conversation_id");
    }

    public override void Down()
    {
        Delete.Index("IX_messages_conversation_id");
    }
}
```

**Status:** OPEN

---

<!--
STATUS DEFINITIONS:
- OPEN: Issue confirmed, not yet being worked on
- IN PROGRESS: Actively being investigated or fixed
- RESOLVED: Fix implemented and verified (include release number)
- NOT A BUG: Investigated, determined to be expected behavior
- WONT FIX: Valid issue but won't be addressed (explain why)
-->
