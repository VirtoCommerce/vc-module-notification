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
{%- comment -%} snippets/button: button_url, button_label, button_style (optional, default 'primary') {%- endcomment -%}
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
