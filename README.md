# Multi-Domain Email & Forms for Umbraco

Multi-domain SMTP and reCAPTCHA support for Umbraco Forms. Perfect for multi-site installations where each domain needs its own email settings and reCAPTCHA keys.

## Features

- **Multi-Domain SMTP** - Different email settings per domain
- **Multi-Domain reCAPTCHA** - Separate reCAPTCHA keys per domain
- **Forms Workflow** - Pre-built Umbraco Forms workflow
- **Automatic Detection** - Detects current domain automatically
- **Fallback Support** - Falls back to default config when needed

## Installation

```bash
dotnet add package DigitalWonderlab.MultiDomainEmail
```

## Requirements

- Umbraco CMS 13
- Umbraco Forms 13
- .NET 8.0

## Configuration

Add to your `appsettings.json`:

```json
{
  "MultiDomainSmtpSettings": {
    "example.com": {
      "From": "noreply@example.com",
      "Host": "smtp.mailgun.org",
      "Port": 587,
      "Username": "postmaster@mg.example.com",
      "Password": "your-password-here",
      "SecureSocketOptions": "StartTls"
    },
    "anotherdomain.com": {
      "From": "info@anotherdomain.com",
      "Host": "smtp.sendgrid.net",
      "Port": 587,
      "Username": "apikey",
      "Password": "your-api-key",
      "SecureSocketOptions": "Auto"
    },
    "default": {
      "From": "noreply@yourdefault.com",
      "Host": "smtp.gmail.com",
      "Port": 587,
      "Username": "your-email@gmail.com",
      "Password": "your-password",
      "SecureSocketOptions": "StartTls"
    }
  },
  "MultiDomainRecaptcha": {
    "example.com": {
      "SiteKey": "6Lc-your-site-key",
      "PrivateKey": "6Lc-your-private-key"
    },
    "anotherdomain.com": {
      "SiteKey": "6Lc-different-site-key",
      "PrivateKey": "6Lc-different-private-key"
    },
    "default": {
      "SiteKey": "6Lc-default-site-key",
      "PrivateKey": "6Lc-default-private-key"
    }
  }
}
```

## Usage

### Using the Umbraco Forms Workflow

1. Create or edit a form in Umbraco backoffice
2. Add the **"Multi-Domain Email"** workflow
3. Configure settings:
   - **Email** - Recipient email address
   - **Subject** - Email subject line
   - **Show All Form Fields** - Include all fields (checkbox)
   - **Custom Message** - Optional message (supports tokens)
   - **Send Copy To Submitter** - Send confirmation email (checkbox)
   - **Submitter Email Field** - Field alias for submitter's email

The workflow automatically uses the correct SMTP settings for the current domain.

### Token Replacement

Use these tokens in subject or custom message:

- `{fieldName}` - Any form field (e.g., `{Name}`, `{Email}`)
- `{formName}` - Form name
- `{submissionDate}` - Submission date/time

Example:
```
Hello {Name}, thank you for contacting us on {submissionDate}.
```

## How It Works

### Domain Detection

- Extracts domain from current HTTP request
- **Automatically strips port numbers** - `localhost:44322` → `localhost`
- **Removes `www.` prefix** - `www.example.com` → `example.com`
- **Case-insensitive matching** - `Example.COM` matches `example.com` config
- Works seamlessly across all environments (localhost, staging, production)

**Important:** Configure domains **without** port numbers in your `appsettings.json`:
- ✅ Correct: `"localhost"`
- ❌ Incorrect: `"localhost:44322"`
- ✅ Correct: `"example.com"`
- ❌ Incorrect: `"example.com:443"`

The package automatically handles all ports for each domain, so one config entry works everywhere.

### Configuration Fallback

The system follows this priority order when looking for SMTP settings:

1. **Domain-specific config** - Exact match (e.g., `example.com`)
2. **`default` config** - Fallback for unmatched domains
3. **Legacy `SmtpSettings`** - Umbraco's standard SMTP config (SMTP only, not reCAPTCHA)

### Local Development

For local testing with tools like MailHog:
- Use `localhost` (without port) in your configuration
- Works with any local port: `:5000`, `:44322`, `:3000`, etc.
- No need to configure multiple entries for different ports

## Customizing Email Templates

Email templates can be customized by modifying the workflow source code:

**File:** `Workflows/MultiDomainEmailWorkflow.cs`

**Methods:**
- `BuildEmailContent()` - Admin notification email template
- `BuildThankYouEmail()` - Submitter confirmation email template

Change HTML structure, styling, or content as needed. Requires rebuilding the package after modifications.

**Methods:**
- `BuildEmailContent()` - Admin email template
- `BuildThankYouEmail()` - Submitter confirmation template

Change styling, layout, or content as needed. Requires rebuilding the package.

## Security

**Never commit passwords!** Use environment variables or secrets:

```bash
# User secrets (development)
dotnet user-secrets set "MultiDomainSmtpSettings:example.com:Password" "your-password"

# Environment variables (production)
export MultiDomainSmtpSettings__example_com__Password="your-password"
```

## Support

- 🐛 [Report Issues](https://github.com/dwlgit/Multi-Domain-Email-Forms/issues)

---

**Made by [Digital Wonderlab](https://digitalwonderlab.com)**