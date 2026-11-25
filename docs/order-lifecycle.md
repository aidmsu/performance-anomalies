# Order lifecycle

1. **Placed** - checkout validation passed and payment is authorised.
2. **Reserved** - stock is reserved in one or more warehouses.
3. **Shipped** - every parcel has a tracking number.
4. **Delivered** - the carrier confirmed delivery.

Orders can be cancelled until they are shipped. Returns start from the delivered state.
