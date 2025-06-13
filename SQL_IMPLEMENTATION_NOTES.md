# SQLite Implementation with Dapper

## Current Status
✅ **SQLite implementation is complete and working** using Dapper instead of EF Core.

## Files Added
- `DapperSavingsAccountRepository.cs` - Dapper-based SQLite repository
- `schema.sql` - Database schema for SQLite tables
- Dapper and Microsoft.Data.Sqlite packages
- Connection string configuration in appsettings.json

## Implementation Details
- **Event Sourcing Pattern**: Transactions are stored and replayed to rebuild account state
- **Optimistic Concurrency**: Uses Version field to prevent concurrent modification conflicts
- **Domain-Friendly**: Works with existing immutable domain classes without modifications
- **Idempotency Support**: Handles duplicate transaction detection via IdempotencyKey

## Database Schema
- `SavingsAccounts` table: Core account data (Id, Balance, InterestRate, CreatedAt, Version)
- `Transactions` table: All account operations with foreign key to accounts
- Indexes on AccountId and IdempotencyKey for performance

## Configuration
- `UseInMemoryDatabase: true` - Uses InMemorySavingsAccountRepository (default)
- `UseInMemoryDatabase: false` - Uses DapperSavingsAccountRepository with SQLite
- Database file: `savings.db` (created automatically)
- All tests pass with both implementations

## Benefits Over EF Core
- No entity mapping complexity
- Works with existing domain design
- Lightweight and fast
- Full control over SQL queries
- Better performance for this use case