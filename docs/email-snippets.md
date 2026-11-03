# Email Snippets

Snippets are small reusable pieces of email HTML, such as a button or an order items table. A template body or a layout calls them with the Liquid `include` tag. Snippet names start with `snippets/`; any other name in an `include` is treated as a layout id.

## Calling a snippet

```liquid
{% include 'snippets/button' button_url: reset_url, button_label: 'Reset password' %}
```

Arguments are named and separated by commas. Positional arguments are not supported in Liquid mode.

## Writing a snippet

Scriban runs an `include` in the same scope as the caller, so its arguments become global variables. They stay set after the snippet returns, and an argument named like a model variable overwrites that variable. Two rules prevent this:

1. Prefix every argument with the snippet name: `button_url`, `button_label`, `button_style`. Model variables never use these names.
2. At the end of the snippet, set every argument to `nil`. An optional argument from one call then does not carry over to the next call.

```liquid
{% comment %} snippets/button: button_url, button_label, button_style (optional, default 'primary') {% endcomment %}
<a href="{{ button_url }}" class="{{ button_style | default: 'primary' }}">{{ button_label }}</a>
{%- assign button_url = nil -%}
{%- assign button_label = nil -%}
{%- assign button_style = nil -%}
```

A snippet can read any model variable, for example `customer_order` or `brand`, without passing it as an argument.

## Alternatives that were checked

These were tested against Scriban 7.2.5 in Liquid mode and rejected:

| Option | Result |
|---|---|
| Positional `include` arguments (`{% include 'snippets/button' url 'Label' %}`) | Parse error |
| Custom `render` function with its own scope, like Shopify `{% render %}` | Liquid mode does not pass named arguments to custom functions |
| Function taking an object literal (`{{ 'snippets/button' \| snippet: { url: x } }}`) | Liquid mode has no object literals |
| Setting variables with `assign` before `include` | Works, but leaks the same way and is harder to read |

## Checking for empty values

In Scriban Liquid mode `x != blank` and `x != empty` are true even when `x` is null. Use `.size > 0`, which is false for null, an empty string and an empty array:

```liquid
{% if brand.logo_url.size > 0 %}<img src="{{ brand.logo_url }}">{% endif %}
```

The `newline_to_br` and `date: '%Y'` filters are not available either. Use `replace: "\n", "<br>"` and `{{ date.now.year }}`.

Write comments as `{% comment %}…{% endcomment %}` without the `-` whitespace markers: `{%- comment -%}` is not recognized and its text ends up in the email.

## Formatting money

`{{ amount | money: currency }}` formats an amount in the notification language with the currency symbol, for example `$1,157.39` for `en-US` and `1.157,39 €` for `de-DE`. Without a currency code it prints the number only. When the server runs in globalization-invariant mode it prints `1,157.39 USD`.

## Shipped snippets

| Snippet | Arguments |
|---|---|
| `eyebrow` | `eyebrow_text` |
| `heading` | `heading_text` |
| `text` | `text_body` |
| `button` | `button_url`, `button_label`, `button_style` (`primary` or `secondary`) |
| `divider` | none |
| `spacer` | `spacer_size` (pixels, 16 by default) |
| `image` | `image_url`, `image_alt`, `image_link` |
| `items` | `items_list` (line items), `items_currency` |
| `totals` | `totals_source` (order or cart) |
| `address` | `address_value`, `address_title` |
| `notice` | `notice_text`, `notice_style` (`info` or `warning`) |
| `tracking` | `tracking_number`, `tracking_carrier`, `tracking_url` |
| `benefits` | `benefits_items` (up to three, separated by `\|`) |

Model values such as product names and addresses are HTML-escaped inside the snippets. Text arguments such as `heading_text` are printed as they are, so they can contain inline HTML.

## Brand

The Default Master and the snippets read the email brand from the `brand` object: `brand.logo_text`, `brand.logo_url`, `brand.logo_width`, `brand.accent_color`, `brand.header_style` (`line`, `dark` or `accent`), `brand.email_width`, `brand.footer_text`, `brand.address`, `brand.social_links` and `brand.utility_links` (each with `name` and `url`), and `brand.show_unsubscribe`.

The brand is set in the `Notifications.Brand.*` settings, globally and per store (store settings, group "Email brand"). When a notification is sent for a store, the renderer takes each value from the store, then from the global setting, then the default. Notifications sent without a store use the global values. If the model already has a `brand` member, for example in the sample data of the template editor, that one is used instead.
