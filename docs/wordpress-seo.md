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

Configure the WordPress category ID in `appsettings.json`:

```json
{
  "WordPress": {
    "CategoryId": 12,
    "RequireCategoryId": true
  }
}
```

The category ID can be found in the WordPress admin category edit URL or from:

```text
GET /wp-json/wp/v2/categories
```

In the WordPress admin panel, open **Posts > Categories**, click the target
category, and inspect the browser URL. The numeric value after `tag_ID=` is the
category ID. For example, `tag_ID=50` means the category ID is `50`.

For Docker deployment, set the same numeric ID in `.env`:

```dotenv
WORDPRESS_CATEGORY_ID=50
WORDPRESS_REQUIRE_CATEGORY_ID=true
```

The Worker validates this category before generating an article and verifies
that WordPress returned the configured category after publishing. An invalid or
missing category stops the run instead of silently publishing an uncategorized
post.

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
from the structured `references` array returned by the selected LLM provider. The news prompt now
requires a title and direct URL for every source. Existing fixture content
without URLs will remain plain text after the placeholders are removed.
