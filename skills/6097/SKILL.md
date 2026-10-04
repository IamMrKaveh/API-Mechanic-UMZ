---
name: sql-queries
description: Writes and fixes SQL — correct joins, aggregation that does not double-count, and queries that use an index. Use this whenever the user is writing a query, mentions SQL, a slow query, EXPLAIN, a join producing wrong counts, or asks how to get some data out of a database. For designing the tables themselves, use data-modeling; for changing them safely, use data-migration.
license: MIT
---

# SQL queries

SQL fails quietly. A query with a wrong join returns rows — just the wrong ones, and nothing
errors. The number looks plausible, someone puts it in a report, and it is wrong for months.

**Verify the row count before trusting the aggregate.** That single habit catches most SQL bugs.

## 1. Say what one row of the result means

Before writing, state the grain: "one row per order" or "one row per customer per month". Almost
every SQL bug is a grain the author did not intend.

Then check it. If you expect one row per order and get 1,400 rows for 1,200 orders, a join
fanned out, and any `SUM` over that result is now inflated.

```sql
SELECT COUNT(*) FROM ...;                       -- vs. what you expect
SELECT order_id, COUNT(*) FROM ... GROUP BY 1 HAVING COUNT(*) > 1;
```

**Done when:** the row count matches the stated grain.

## 2. Get the joins right

- **Know which side can be missing.** `INNER` drops rows with no match, often silently
  excluding exactly the cases you care about, like customers with no orders.
- **A `LEFT JOIN` with a condition on the right table in `WHERE` becomes an inner join.** The
  filter removes the null rows the join just produced. Put the condition in the `ON` clause.
- **Joining a one-to-many before aggregating multiplies your measures.** Aggregate in a subquery
  or CTE first, then join the result.
- **Never comma-join.** A missing condition becomes a cross join with no warning.

**Done when:** each join's cardinality is deliberate.

## 3. Handle NULL deliberately

NULL is not a value and does not behave like one:

- `NULL = NULL` is not true. Use `IS NULL`, or `IS DISTINCT FROM`
- `WHERE status != 'done'` **excludes rows where status is NULL**. This is the single most
  common silent filter bug
- Aggregates skip NULLs — `AVG` over a column with nulls averages fewer rows than you think
- `COUNT(*)` counts rows; `COUNT(col)` counts non-null values. Different numbers
- `NOT IN` with a NULL in the subquery returns nothing at all

**Done when:** every nullable column in a predicate has had its NULL behaviour considered.

## 4. Write for the index

```sql
EXPLAIN ANALYZE SELECT ...;
```

Read the plan, not the query. Look for sequential scans on large tables, and for estimated rows
far from actual — that means stale statistics and a plan chosen on bad information.

Things that stop an index being used:
- **A function on the indexed column:** `WHERE DATE(created_at) = ...` cannot use an index on
  `created_at`. Use a range instead
- **A leading wildcard:** `LIKE '%foo'`
- **Type mismatch** forcing an implicit cast
- **`OR` across different columns:** often better as `UNION ALL`

Composite indexes only help when the query filters on a leading prefix of the columns.

**Done when:** the plan shows an index scan where you expect one.

## 5. Prefer clarity, and mind the known traps

- **CTEs for readability**, and know whether your database materialises them — in some versions
  a CTE is an optimisation fence, in others it is inlined
- **Window functions** instead of self-joins for running totals and rankings
- **`EXISTS` over `IN`** for a subquery on a large set, and it is NULL-safe
- **Never `SELECT *`** in anything saved. It breaks when columns are added and pulls data you do
  not need
- **Integer division truncates:** a classic wrong-percentage bug. Cast first
- **Beware `BETWEEN` on timestamps.** It includes the endpoint, so a day range double-counts
  midnight. Use `>= start AND < end`

**Done when:** the query says what it means and avoids the traps above.

## 6. Verify before you trust it

- Run it against a known small case where you can count by hand
- Check totals against an independently computed number
- Check the boundaries — the first and last day of a range, the customer with no orders, the
  order with no items
- For anything that will be reported, have someone else reproduce the number a different way

**Done when:** the result agrees with an independent calculation.

## Never build SQL by string concatenation

Parameterise, always, including for internal tools and one-off scripts. This is the
straightforward path to SQL injection, and "it's internal" is how most such holes are justified
before they are found. See `security-analysis`.
