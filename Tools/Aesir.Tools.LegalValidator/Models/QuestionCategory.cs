using System.Text.Json.Serialization;

namespace Aesir.Tools.LegalValidator.Models;

/// <summary>
/// Categories of legal questions for validation testing.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum QuestionCategory
{
    FactualDefinitional,
    Procedural,
    Analytical,
    JurisdictionSpecific,
    NuancedEdgeCase,
    EthicsProfessional,
    Contracts,
    Torts,
    Criminal,
    Constitutional,
    Corporate
}

/// <summary>
/// Difficulty level of a legal question.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DifficultyLevel
{
    Basic,
    Intermediate,
    Advanced
}

/// <summary>
/// Overall grade for an evaluation result.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OverallGrade
{
    APlus,
    A,
    AMinus,
    BPlus,
    B,
    BMinus,
    CPlus,
    C,
    CMinus,
    D,
    F
}
