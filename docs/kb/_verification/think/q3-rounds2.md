# What happens when a basket is checked out?

## Answer

1. The Shopping web app sends checkout data with `POST /basket-service/basket/checkout` and expects a `CheckoutBasketResponse`. [src-webapps-shopping-web-services-ibasketservice-cs]
2. The checkout payload carries the username/customer ID, total price, shipping and contact details, card details, and payment method; matching models exist in both the web app and Basket API. [src-webapps-shopping-web-models-basket-basketcheckoutmodel-cs] [src-services-basket-basket-api-dtos-basketcheckoutdto-cs]
3. The Basket API exposes a checkout endpoint backed by `CheckoutBasketCommandHandler`, although the retrieved excerpts do not show the handler’s internal operations. [src-services-basket-basket-api-basket-checkoutbasket-checkoutbasketendpoints-cs] [src-services-basket-basket-api-basket-checkoutbasket-checkoutbaskethandler-cs]
4. The codebase defines a `BasketCheckoutEvent`; on the Ordering side, `BasketCheckoutEventHandler` consumes that integration event and maps it to a create-order command. [src-buildingblocks-buildingblocks-messaging-events-basketcheckoutevent-cs] [src-services-ordering-ordering-application-orders-eventhandlers-integration-basketcheckouteventhandler-cs]
5. A newly constructed order defaults to `Pending` status. [src-services-ordering-ordering-domain-models-order-cs]
6. The web app has an order-submitted confirmation state displaying “Your order submitted successfully.” [src-webapps-shopping-web-pages-confirmation-cshtml-cs]

## Gaps
- The retrieved `CheckoutBasketCommandHandler` excerpt does not expose whether checkout loads or validates the basket, publishes the integration event, deletes the basket, or handles failure atomically.
- The retrieved checkout endpoint excerpt does not expose its route mapping, validation rules, status codes, or response contents.
- The retrieved Ordering event-handler excerpt does not show whether the mapped create-order command is executed successfully or how the order and its items are persisted.
- The retrieved web checkout page does not include its submission handler, so the conditions for navigating to the success confirmation are unknown.

---
Model: openai:gpt-5.6-sol | Pages: 40 | Takes: 0 | Graph: 0 | Citations: 9 | Cost: $0.0661
