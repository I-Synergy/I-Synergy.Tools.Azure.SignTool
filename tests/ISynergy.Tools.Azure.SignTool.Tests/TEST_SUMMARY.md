# Unit Tests for Azure Credential Type Feature

## Test Summary

Created comprehensive unit tests for the new Azure credential type authentication functionality. All tests use MSTest framework and follow existing project conventions.

## Test Files Created

### 1. AzureCredentialTypeTests.cs
Tests for the `AzureCredentialType` enumeration.

**Test Coverage:**
- ✅ All 9 credential types are defined correctly
- ✅ Credential type names match expected values
- ✅ Enum parsing works case-insensitively
- ✅ Invalid values fail to parse as expected

**Tests (11 total):**
- `AllCredentialTypesShouldBeDefined` - Verifies all 9 types exist
- `CredentialTypeNamesShouldMatchExpectedValues` - Verifies naming conventions
- `EnumParseShouldWorkCaseInsensitive` - Tests case-insensitive parsing (9 data rows)
- `EnumParseShouldBeCaseInsensitive` - Additional case sensitivity tests
- `EnumParseShouldFailForInvalidValues` - Tests rejection of invalid values (3 data rows)

### 2. CredentialTypeValidationTests.cs
Tests for command-line validation logic with the new credential type parameter.

**Test Coverage:**
- ✅ Validation requires one authentication method
- ✅ Mutual exclusion between --azure-credential-type and legacy flags
- ✅ ClientSecretCredential requires client ID, secret, and tenant ID
- ✅ AccessTokenCredential requires access token
- ✅ Other credential types don't require extra parameters
- ✅ Legacy managed identity flag still works

**Tests (16 total):**
- `WhenNoAuthenticationMethodProvided_ShouldShowError` - No auth method provided
- `WhenCredentialTypeProvidedWithManagedIdentityFlag_ShouldShowError` - Mutual exclusion with managed identity
- `WhenCredentialTypeProvidedWithClientId_ShouldShowError` - Mutual exclusion with client ID
- `WhenClientSecretCredentialWithoutClientId_ShouldShowError` - Missing client ID
- `WhenClientSecretCredentialWithoutClientSecret_ShouldShowError` - Missing client secret
- `WhenClientSecretCredentialWithoutTenantId_ShouldShowError` - Missing tenant ID
- `WhenAccessTokenCredentialWithoutToken_ShouldShowError` - Missing access token
- `WhenValidCredentialTypeWithoutExtraParams_ShouldPassValidation` - Valid credential types (7 data rows)
- `WhenManagedIdentityFlagUsedAlone_ShouldPassValidation` - Legacy flag compatibility

### 3. AzureKeyVaultSignConfigurationSetTests.cs
Tests for the configuration data structure.

**Test Coverage:**
- ✅ All properties can be set correctly
- ✅ Null credential type is allowed
- ✅ All 9 credential types can be assigned
- ✅ Minimal configuration works
- ✅ Configuration accepts both legacy and new options (for validation elsewhere)

**Tests (14 total):**
- `ConfigurationSet_ShouldAcceptAllProperties` - All properties work
- `ConfigurationSet_WithNullCredentialType_ShouldAllowNull` - Nullable credential type
- `ConfigurationSet_ShouldAcceptAllCredentialTypes` - Each credential type (9 data rows)
- `ConfigurationSet_WithManagedIdentityTrue_ShouldWork` - Legacy flag support
- `ConfigurationSet_CanSetBothManagedIdentityAndCredentialType` - Configuration allows both
- `ConfigurationSet_WithMinimalProperties_ShouldWork` - Minimal valid config

## Test Execution

```powershell
dotnet test --verbosity normal
```

**Results:**
- Total Tests: 108 (including existing tests)
- Passed: 108
- Failed: 0
- Skipped: 0
- Duration: ~4.8s

## Test Approach

### What Was Tested
1. **Enumeration Values** - All credential types properly defined
2. **Validation Logic** - Command-line parameter validation through public API
3. **Configuration** - Data structure accepts all valid combinations
4. **Integration** - Tests execute through Program.Main() to test real validation

### What Was Not Tested (Internal Implementation)
- Direct instantiation of internal `AzureKeyVaultConfigurationDiscoverer` class
- Direct Azure SDK credential creation (would require mocking Azure SDK)
- Actual Azure Key Vault connectivity (integration test, not unit test)

### Testing Strategy
- **Public API Testing**: Tests use `Program.Main()` with various command-line arguments
- **Validation Testing**: Verify error messages and exit codes
- **Data Structure Testing**: Direct testing of configuration classes
- **MSTest Framework**: Following existing project conventions
- **Data-Driven Tests**: Using `[DataRow]` for multiple similar scenarios

## Code Coverage Areas

✅ **Fully Covered:**
- Enumeration definition and parsing
- Command-line validation logic
- Configuration data structures
- Mutual exclusion rules
- Required parameter validation

⚠️ **Partial Coverage (Internal/Integration):**
- Credential creation logic (internal class)
- Logging output (tested through integration, not unit tests)
- Azure SDK interactions (would need integration tests)

## Running Specific Test Classes

```powershell
# Run only credential type enum tests
dotnet test --filter FullyQualifiedName~AzureCredentialTypeTests

# Run only validation tests
dotnet test --filter FullyQualifiedName~CredentialTypeValidationTests

# Run only configuration tests
dotnet test --filter FullyQualifiedName~AzureKeyVaultSignConfigurationSetTests
```

## Future Test Enhancements

Consider adding:
1. **Integration Tests**: Test actual Azure Key Vault connectivity with test credentials
2. **Logging Tests**: If `InternalsVisibleTo` is added, test logging output
3. **Performance Tests**: Test credential creation performance
4. **E2E Tests**: Full signing workflow with different credential types
