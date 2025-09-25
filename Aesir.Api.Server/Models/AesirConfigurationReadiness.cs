using Aesir.Common.Models;

namespace Aesir.Api.Server.Models;

/// <summary>
/// Represents the server-side configuration readiness model that extends the base
/// configuration readiness functionality with server-specific implementations.
/// </summary>
/// <remarks>
/// This class inherits from AesirConfigurationReadinessBase and is used by the
/// ConfigurationController to return detailed readiness information to clients.
/// </remarks>
public class AesirConfigurationReadiness:AesirConfigurationReadinessBase;
