---
name: data-modeling
description: Designs the shape of stored data — tables, keys, relationships, constraints, and what the database should enforce rather than the application. Use this whenever the user is creating tables, designing a schema, choosing keys or column types, asking how to represent something in a database, or picking between SQL and a document store. For changing an existing schema safely, use data-migration; for querying it, use sql-queries.
license: MIT
---

# Data modeling

Schema outlives application code, usually by years. Code gets rewritten; the data stays, and
everything written since inherits whatever shape you chose. Wrong data is also far harder to fix
than wrong logic — you can deploy a fix for logic, but bad rows are permanent unless you can
reconstruct the truth.

**Push correctness into the database.** Every rule the schema enforces is a rule the application
cannot violate, including from a script someone runs at 2am.

## 1. Model the real relationships

Get cardinality right before anything else — it determines the whole structure.

- **One-to-many:** a foreign key on the many side
- **Many-to-many:** a join table. Give it its own key and any attributes of the relationship
  itself
- **One-to-one:** usually the same table, unless you are genuinely splitting for access or
  size reasons

Ask whether it is *really* one-to-one or one-to-many. "A user has one address" becomes false the
moment someone needs a billing address, and retrofitting is a migration. The question to ask is
whether the business could ever say "actually, two" — if so, model it as many now.

**Done when:** each relationship's cardinality reflects the domain, not the current feature.

## 2. Choose types precisely

The type is a constraint, and a loose type is a bug that has not happened yet.

- **Money is never a float.** Use a decimal type or integer minor units. Floating point silently
  loses cents and the errors accumulate
- **Timestamps with timezone**, stored UTC. A naive timestamp is ambiguous forever, and you
  cannot recover the intent later
- **Dates when it is a date:** a birthday is not an instant
- **Native enum or a lookup table**, not a free string. A `status` column with `'active'`,
  `'Active'`, and `'ACTIVE'` is inevitable otherwise
- **Text over varchar(n)** unless the limit is a real business rule. An arbitrary 255 is a future
  truncation bug
- **JSON columns only for genuinely unstructured data.** JSON is where schema design goes to
  hide, and you cannot constrain or index it as well

**Done when:** no column can hold a value that is meaningless for it.

## 3. Constrain everything you can

- **`NOT NULL` by default.** Make nullability the exception you justify. A nullable column has a
  third state every query must handle
- **Foreign keys with explicit `ON DELETE`** behaviour. Decide cascade, restrict, or set null
  deliberately — the default is rarely what you want
- **Unique constraints** on anything that must be unique. Application-level uniqueness checks
  lose the race under concurrency, always
- **Check constraints** for ranges and invariants — a quantity that cannot be negative, an end
  date after a start date
- **Defaults** for columns with an obvious one

Nearly every "how did this row get like that?" is a missing constraint.

**Done when:** an invalid row cannot be inserted by any path.

## 4. Choose keys carefully

- **A surrogate primary key:** auto-increment or UUID — for almost everything. Natural keys
  change, and a changing primary key is painful
- **Still add the unique constraint** on the natural key. The surrogate is for joins; the
  constraint is for correctness
- **UUID v7 or ULID over v4** if you use UUIDs — random keys fragment index locality and hurt
  insert performance at scale
- **Never expose sequential IDs** in URLs where enumeration matters. See `security-analysis`

**Done when:** keys are stable and uniqueness is enforced.

## 5. Normalise first, denormalise on evidence

Normalise until each fact lives in one place. Then denormalise only where a measured problem
requires it, and write down which copy is authoritative and how it stays in sync.

Duplicated data without a stated sync mechanism will diverge. Not might — will.

Legitimate exceptions: a value that must be historically frozen — the price at time of order is
genuinely not the current product price, and copying it is correct modelling, not denormalisation.

**Done when:** every duplicated value has an owner and a sync path.

## 6. Design for time and deletion

Two things retrofitted painfully:

- **History:** do you need to know what a value *was*? Decide now. Adding history later means
  you have lost everything before the change. Options: audit table, event log, or valid-from and
  valid-to columns
- **Deletion:** hard delete, or soft delete with a flag? Soft deletes leak into every query and
  every unique constraint. Choose deliberately, and know your retention and erasure obligations

**Done when:** the answers to "what was this last month?" and "what does deleted mean?" are
explicit.

## Report

State the entities, their relationships and cardinality, what the database enforces, and what
you deliberately left to the application. Note what would be expensive to change later — that is
the part worth a second opinion before it ships.
