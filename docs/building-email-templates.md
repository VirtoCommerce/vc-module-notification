# Building Email Templates on the Default Master

Every email has two parts:

* **The Master layout** (`Default`) holds what all emails share: the header with the logo, the footer with the address and links, the width and the brand colors. It ships with this module.
* **The template** of a notification type holds only what is unique to that email: the subject and the body, for example the order items and totals.

Change the brand once, and it applies to every email that uses the Master. Change a template, and only that email changes.

## How a template reaches the Master

The body puts its HTML into a variable named `content`. The renderer then includes the layout, which prints `{{ content }}` between the header and the footer:

```liquid
{% capture content %}
  {% include 'snippets/heading' heading_text: 'Thanks for your order!' %}
  {% include 'snippets/items' items_list: customer_order.items, items_currency: customer_order.currency %}
{% endcapture %}
```

The layout and the body share variables, so the layout can read the model (`customer_order`, `customer`) and the body can set values for the layout:

* `preheader`: the hidden preview text that mail clients show next to the subject, for example `{% assign preheader = 'We have received your order' %}`.
* `unsubscribe_url`: shown in the footer when the brand setting *Show unsubscribe link* is on. Only marketing emails such as abandoned cart need it.

A body written this way renders empty without a layout, so a template on the Master must always have one.

## Blocks: snippets

Build the body from snippets, such as `heading`, `text`, `button`, `items`, `totals`, `address` and `notice`, instead of writing table markup. Snippets work in Outlook, Gmail and mobile clients, follow the brand colors, and HTML-escape model values. [Email Snippets](email-snippets.md) lists them with their arguments and explains the calling convention.

Text arguments are printed as they are. Build text that mixes literal words and model values with `capture`, and escape the model values:

```liquid
{% capture order_heading %}Thanks for your order, {{ customer.first_name | escape }}!{% endcapture %}
{% include 'snippets/heading' heading_text: order_heading %}
```

Format amounts with the `money` filter: `{{ customer_order.total | money: customer_order.currency }}`.

## Shipping a template in a module

1. Put the files in the module's `NotificationTemplates` folder: `<NotificationType>_body.html`, `<NotificationType>_subject.txt` and `<NotificationType>_sample.json`. Add a language to the name for translations, for example `OrderCreateEmailNotification_body.de-DE.html`.
2. Write the body as above. Keep it under 20 KB; with the Master the whole email must stay under Gmail's 102 KB clipping limit.
3. Fill the sample with realistic data for every block the body uses. The template editor and the layout preview render it.
4. Register the notification with the Master:

   ```csharp
   registrar.RegisterNotification<OrderCreateEmailNotification>()
       .WithTemplatesFromPath(defaultTemplatesDirectory)
       .WithLayout(ModuleConstants.DefaultLayoutName);
   ```

5. Raise the `VirtoCommerce.Notifications` dependency in `module.manifest` to the release that ships the Master. With an older Notifications module there is no Master and the email renders empty.

The Master is not the default layout: only templates registered with `.WithLayout(...)` use it. Templates that administrators have edited in the database keep rendering as before.

## Brand

The brand comes from the *Email brand* settings: logo, accent color, header style, width, footer text, address, social and utility links. Set them globally in the platform settings and override any of them per store in the store settings. An email sent for a store uses that store's values. See [Email Snippets: Brand](email-snippets.md#brand) for the variables.

## Adopting the Master in an edited template

A template that was edited in the Admin UI before the upgrade is a full HTML page and keeps working without a layout. To move it to the Master:

1. Open the template and select `Default` in the *Layout* field.
2. Replace the page with the body only, wrapped in `{% capture content %}…{% endcapture %}`. Remove the header, footer and `<html>`/`<body>` tags; the Master adds them.
3. Check the *Preview* tab, then save.

Or use *Restore* in the template editor to return to the template shipped with the module.

## Editing the Master

To change the look for everyone on an installation, open *Notification layouts* → *Default* in the Admin UI. The *Preview* command renders any email type inside the layout as you edit it, with the brand of the store you select. *Reset to default* restores the shipped version.

To change the shipped Master in this module, edit the MJML source in `src/VirtoCommerce.NotificationsModule.Web/email-src/layouts/Default.mjml` and run `npm run build:email` in that folder. The build writes `NotificationLayouts/Default.html`, which is committed; never edit that file by hand.

## Liquid pitfalls

Scriban's Liquid mode differs from Shopify Liquid in a few places that templates hit often:

* `x != blank` and `x != empty` are true even when `x` is null. Use `x.size > 0`.
* `{%- comment -%}` with whitespace markers is not recognized and its text ends up in the email. Use `{% comment %}`.
* `newline_to_br` and `date: '%Y'` are not available. Use `replace: "\n", "<br>"` and `{{ date.now.year }}`.

For general email HTML rules (inline styles, tables for layout, Outlook quirks), see [Tips and tricks for creating email templates](tips-and-tricks-for-creating-email-templates.md).
