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
- Extracts domain from request URL
- Removes `www.` prefix automatically
- Case-insensitive matching

### Configuration Fallback
1. Domain-specific config (e.g., `example.com`)
2. `default` config
3. Legacy `SmtpSettings` (SMTP only)

## Customizing Email Templates

Email templates can be customized by editing:

**File:** `Workflows/MultiDomainEmailWorkflow.cs`

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

- 🐛 [Report Issues](https://github.com/digitalwonderlab/umbraco-multidomain-email/issues)

---

**Made by [Digital Wonderlab](https://digitalwonderlab.com)**
