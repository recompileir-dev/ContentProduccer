# WordPress SEO Publishing

## WordPress Post Behavior

The WordPress publisher now:

- Uploads only the first generated image.
- Sets the image title, caption, and alt text.
- Uses the image as featured media.
- Inserts the image at the beginning of the post content.
- Removes OpenAI citation placeholders such as `:contentReference[...]`.
- Adds configured category IDs.
- Adds configured internal links.
- Sends the generated meta description as the WordPress excerpt.

## Categories

Configure WordPress category IDs in `appsettings.json`:

```json
{
  "WordPress": {
    "CategoryIds": [12, 18]
  }
}
```

The category ID can be found in the WordPress admin category edit URL or from:

```text
GET /wp-json/wp/v2/categories
```

## Internal Links

Configure internal URLs that should be appended to the post:

```json
{
  "WordPress": {
    "InternalLinkUrls": [
      "https://example.com/",
      "https://example.com/blog/"
    ]
  }
}
```

## Yoast SEO Meta Fields

Yoast SEO focus keyphrase, SEO title, and meta description are stored as
protected WordPress post meta. WordPress REST API can only write meta fields
that are registered with `show_in_rest`.

The application supports these default Yoast keys:

```json
{
  "WordPress": {
    "SendSeoMeta": false,
    "FocusKeyphraseMetaKey": "_yoast_wpseo_focuskw",
    "SeoTitleMetaKey": "_yoast_wpseo_title",
    "MetaDescriptionMetaKey": "_yoast_wpseo_metadesc"
  }
}
```

Keep `SendSeoMeta` set to `false` until the site exposes these fields to the
REST API. One way is to add a small site plugin:

```php
<?php
/**
 * Plugin Name: Content Producer SEO REST Fields
 */

add_action('init', function () {
    $keys = [
        '_yoast_wpseo_focuskw',
        '_yoast_wpseo_title',
        '_yoast_wpseo_metadesc',
    ];

    foreach ($keys as $key) {
        register_post_meta('post', $key, [
            'type' => 'string',
            'single' => true,
            'show_in_rest' => true,
            'auth_callback' => function () {
                return current_user_can('edit_posts');
            },
        ]);
    }
});
```

After activating the plugin, set:

```json
"SendSeoMeta": true
```

Test this on a staging site first. SEO plugins may change their internal meta
keys or behavior between versions.

## References

The code removes invalid citation placeholders and builds clickable source links
from the structured `references` array returned by OpenAI. The news prompt now
requires a title and direct URL for every source. Existing fixture content
without URLs will remain plain text after the placeholders are removed.
