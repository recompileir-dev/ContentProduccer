# OpenAI Content Generation

## Execution Flow

On each scheduled or command-line run:

1. The article prompt is read from a file on the host.
2. The prompt is sent to the OpenAI Responses API.
3. When `EnableWebSearch` is enabled, the model researches fresh news before writing.
4. The generated article is inserted into the editable image prompt template.
5. One horizontal website article image is generated with the OpenAI Image API.
6. The article, social caption, and image are passed directly to the publishing services.

The Worker does not save generated article or image files to disk.

## API Key

Do not commit the OpenAI API key to Git. Set it on the host:

```bash
export OPENAI_API_KEY="your-api-key"
```

OpenAI API billing is separate from a free ChatGPT account. The environment
variable name can be changed with `OpenAI:ApiKeyEnvironmentVariable`.

## Settings

```json
{
  "OpenAI": {
    "ApiKeyEnvironmentVariable": "OPENAI_API_KEY",
    "BaseUrl": "https://api.openai.com/v1/",
    "ArticleModel": "gpt-5-mini",
    "ImageModel": "gpt-image-1-mini",
    "EnableWebSearch": true,
    "ImageSize": "1536x1024",
    "ImageQuality": "low",
    "PromptFilePath": "prompts/article-news-fa.md",
    "ImagePromptFilePath": "prompts/article-image-fa.md"
  }
}
```

The image prompt requests a composition suitable for display at 790 pixels
wide. The API request uses `1536x1024`, a supported landscape output size.

## Prompt Files

- `prompts/article-simple-fa.md`: simple article prompt for local testing
- `prompts/article-news-fa.md`: news prompt that examines two or three fresh AI-life-impact news items
- `prompts/article-image-fa.md`: editable website image prompt

Change `OpenAI:PromptFilePath` to select the article prompt. Change
`OpenAI:ImagePromptFilePath` to select or customize the image prompt. Prompt
files are copied during build and publish, so they can be edited on the host
without changing application code.

The image prompt supports these placeholders:

- `{{title}}`
- `{{focusKeyphrase}}`
- `{{articleHtml}}`

## Structured Article Output

The article prompt must return valid JSON containing:

- `title`
- `articleHtml`
- `instagramCaption`
- `focusKeyphrase`
- `seoTitle`
- `metaDescription`
- `references`

The generated image remains in application memory until it is uploaded to the
WordPress Media Library.
