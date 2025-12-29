using Aesir.Infrastructure.Data;
using Aesir.Modules.Research.Models;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Repository implementation for managing research team configurations.
/// </summary>
public class ResearchTeamRepository(
    ILogger<ResearchTeamRepository> logger,
    IDbContext dbContext) : IResearchTeamRepository
{
    static ResearchTeamRepository()
    {
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<Guid>?>());
    }

    /// <inheritdoc />
    public async Task<ResearchTeam?> GetByIdAsync(Guid id)
    {
        const string teamSql = @"
            SELECT
                id as Id,
                user_id as UserId,
                name as Name,
                description as Description,
                is_active as IsActive,
                created_at as CreatedAt,
                updated_at as UpdatedAt
            FROM aesir.aesir_research_team
            WHERE id = @Id";

        const string membersSql = @"
            SELECT
                id as Id,
                research_team_id as ResearchTeamId,
                agent_id as AgentId,
                role as Role,
                is_active as IsActive,
                override_temperature as OverrideTemperature
            FROM aesir.aesir_research_team_member
            WHERE research_team_id = @TeamId";

        return await dbContext.UnitOfWorkAsync(async connection =>
        {
            var team = await connection.QueryFirstOrDefaultAsync<ResearchTeam>(teamSql, new { Id = id });
            if (team != null)
            {
                team.Members = (await connection.QueryAsync<ResearchTeamMember>(membersSql, new { TeamId = id })).ToList();
            }
            return team;
        });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ResearchTeam>> GetByUserIdAsync(string userId)
    {
        const string teamSql = @"
            SELECT
                id as Id,
                user_id as UserId,
                name as Name,
                description as Description,
                is_active as IsActive,
                created_at as CreatedAt,
                updated_at as UpdatedAt
            FROM aesir.aesir_research_team
            WHERE user_id = @UserId
            ORDER BY created_at DESC";

        const string membersSql = @"
            SELECT
                id as Id,
                research_team_id as ResearchTeamId,
                agent_id as AgentId,
                role as Role,
                is_active as IsActive,
                override_temperature as OverrideTemperature
            FROM aesir.aesir_research_team_member
            WHERE research_team_id = ANY(@TeamIds)";

        return await dbContext.UnitOfWorkAsync(async connection =>
        {
            var teams = (await connection.QueryAsync<ResearchTeam>(teamSql, new { UserId = userId })).ToList();

            if (teams.Count > 0)
            {
                var teamIds = teams.Select(t => t.Id).ToArray();
                var members = await connection.QueryAsync<ResearchTeamMember>(membersSql, new { TeamIds = teamIds });
                var membersByTeam = members.GroupBy(m => m.ResearchTeamId).ToDictionary(g => g.Key, g => g.ToList());

                foreach (var team in teams)
                {
                    team.Members = membersByTeam.TryGetValue(team.Id, out var teamMembers) ? teamMembers : [];
                }
            }

            return teams;
        });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ResearchTeam>> GetActiveByUserIdAsync(string userId)
    {
        const string teamSql = @"
            SELECT
                id as Id,
                user_id as UserId,
                name as Name,
                description as Description,
                is_active as IsActive,
                created_at as CreatedAt,
                updated_at as UpdatedAt
            FROM aesir.aesir_research_team
            WHERE user_id = @UserId AND is_active = true
            ORDER BY created_at DESC";

        const string membersSql = @"
            SELECT
                id as Id,
                research_team_id as ResearchTeamId,
                agent_id as AgentId,
                role as Role,
                is_active as IsActive,
                override_temperature as OverrideTemperature
            FROM aesir.aesir_research_team_member
            WHERE research_team_id = ANY(@TeamIds)";

        return await dbContext.UnitOfWorkAsync(async connection =>
        {
            var teams = (await connection.QueryAsync<ResearchTeam>(teamSql, new { UserId = userId })).ToList();

            if (teams.Count > 0)
            {
                var teamIds = teams.Select(t => t.Id).ToArray();
                var members = await connection.QueryAsync<ResearchTeamMember>(membersSql, new { TeamIds = teamIds });
                var membersByTeam = members.GroupBy(m => m.ResearchTeamId).ToDictionary(g => g.Key, g => g.ToList());

                foreach (var team in teams)
                {
                    team.Members = membersByTeam.TryGetValue(team.Id, out var teamMembers) ? teamMembers : [];
                }
            }

            return teams;
        });
    }

    /// <inheritdoc />
    public async Task<ResearchTeam> AddAsync(ResearchTeam team)
    {
        if (team.Id == Guid.Empty)
            team.Id = Guid.NewGuid();

        var now = DateTime.UtcNow;
        team.CreatedAt = now;
        team.UpdatedAt = now;

        const string teamSql = @"
            INSERT INTO aesir.aesir_research_team
                (id, user_id, name, description, is_active, created_at, updated_at)
            VALUES
                (@Id, @UserId, @Name, @Description, @IsActive, @CreatedAt, @UpdatedAt)";

        const string memberSql = @"
            INSERT INTO aesir.aesir_research_team_member
                (id, research_team_id, agent_id, role, is_active, override_temperature)
            VALUES
                (@Id, @ResearchTeamId, @AgentId, @Role, @IsActive, @OverrideTemperature)";

        await dbContext.UnitOfWorkAsync(async connection =>
        {
            await connection.ExecuteAsync(teamSql, team);

            if (team.Members != null)
            {
                foreach (var member in team.Members)
                {
                    if (member.Id == Guid.Empty)
                        member.Id = Guid.NewGuid();
                    member.ResearchTeamId = team.Id;
                    await connection.ExecuteAsync(memberSql, member);
                }
            }
        }, withTransaction: true);

        logger.LogInformation("Created research team {TeamId} with {MemberCount} members",
            team.Id, team.Members?.Count ?? 0);

        return team;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(ResearchTeam team)
    {
        team.UpdatedAt = DateTime.UtcNow;

        const string teamSql = @"
            UPDATE aesir.aesir_research_team
            SET user_id = @UserId,
                name = @Name,
                description = @Description,
                is_active = @IsActive,
                updated_at = @UpdatedAt
            WHERE id = @Id";

        const string deleteMembersSql = @"
            DELETE FROM aesir.aesir_research_team_member
            WHERE research_team_id = @TeamId";

        const string memberSql = @"
            INSERT INTO aesir.aesir_research_team_member
                (id, research_team_id, agent_id, role, is_active, override_temperature)
            VALUES
                (@Id, @ResearchTeamId, @AgentId, @Role, @IsActive, @OverrideTemperature)";

        var rowsAffected = await dbContext.UnitOfWorkAsync(async connection =>
        {
            var result = await connection.ExecuteAsync(teamSql, team);

            // Delete and re-insert members for simplicity
            await connection.ExecuteAsync(deleteMembersSql, new { TeamId = team.Id });

            if (team.Members != null)
            {
                foreach (var member in team.Members)
                {
                    if (member.Id == Guid.Empty)
                        member.Id = Guid.NewGuid();
                    member.ResearchTeamId = team.Id;
                    await connection.ExecuteAsync(memberSql, member);
                }
            }

            return result;
        }, withTransaction: true);

        logger.LogInformation("Updated research team {TeamId}", team.Id);

        return rowsAffected > 0;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(Guid id)
    {
        // Members are deleted via cascade
        const string sql = @"
            DELETE FROM aesir.aesir_research_team
            WHERE id = @Id";

        var rowsAffected = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, new { Id = id }), withTransaction: true);

        logger.LogInformation("Deleted research team {TeamId}", id);

        return rowsAffected > 0;
    }
}
