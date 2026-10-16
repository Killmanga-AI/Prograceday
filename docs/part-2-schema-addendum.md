# Part 2 schema addendum

Part 2 retains the six Part 1 tables and relationships. It adds two columns to
`Events` to meet the Part 2 requirement that every event records its overall
distance and activity type:

| Column | SQL Server type | Rule |
|---|---|---|
| `EventDistanceKm` | `DECIMAL(6,2)` | Greater than zero |
| `EventType` | `VARCHAR(10)` | `Run`, `Walk`, or `Cycle` |

Category `DistanceKm` remains unchanged. It represents the particular distance
or category offered by an event; `EventDistanceKm` represents the event's
overall advertised route distance. This distinction allows an event to offer
multiple categories.

The EF Core model maps existing text/date fields to the Part 1 SQL Server types
(`VARCHAR` and `DATETIME`) and uses `NO ACTION` foreign keys to match the SQL
script's relationship behavior. The migration from the earlier Part 2 scaffold
backfills event distance with the maximum category distance when available (or
`1.00` if no category exists). It infers `Cycle` or `Walk` when the event name
contains that word; other existing events are marked `Run`. Review those
inferred values for existing data before production use.

`docs/ERD.png` is retained unchanged as the submitted Part 1 ERD. The additions
above are the Part 2 extension and do not add or remove any tables.

The SQL setup script intentionally drops and recreates its tables. Do not run it
against an existing or valuable database. Use EF migrations with a new database,
or take a backup and establish a deliberate migration baseline first.
