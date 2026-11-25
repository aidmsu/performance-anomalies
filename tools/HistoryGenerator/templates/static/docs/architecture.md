# Architecture

Storefront is a small e-commerce domain library. It is organised by business area:

| Folder | Responsibility |
|---|---|
| `Billing` | Invoices and their PDF rendering |
| `Cart`, `Checkout` | Cart totals and checkout validation |
| `Catalog`, `Search` | Product catalog, imports, snapshots and search |
| `Inventory` | Stock reservations, warehouse allocation, synchronisation |
| `Orders` | Order numbers, order API payloads, gift wrapping |
| `Payments`, `Fraud` | Payment authorisation and fraud scoring |
| `Pricing`, `Promotions` | Price calculation, currencies, promotions and coupons |
| `Shipping`, `Tax` | Shipping quotes and labels, tax tables and jurisdictions |
| `Infrastructure` | Cost primitives that stand in for the database, storage, network and CPU work |

There is no real database, disk or network. Components describe their external work through the cost
primitives in `Infrastructure`, which spend a precise amount of wall-clock time.
