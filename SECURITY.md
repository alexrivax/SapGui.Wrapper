# Security Policy

## Supported versions

| Version      | Supported |
| ------------ | --------- |
| 1.x (latest) | ✅        |

## Reporting a vulnerability

Please **do not** open a public GitHub issue for security vulnerabilities.

Send a private report to the maintainer via GitHub's
[Security Advisories](https://github.com/alexrivax/SapGui.Wrapper/security/advisories/new)
feature (Repo → Security → Advisories → New draft advisory).

### What to include

- A description of the vulnerability and its potential impact
- Steps to reproduce
- Suggested fix if you have one

### What to expect

- Acknowledgement within 7 days
- A fix or mitigation in the next release once confirmed

## Scope

This library is a thin late-binding COM wrapper. It executes code inside a running SAP GUI process on the local machine. The primary security considerations are:

- **Input validation**: component IDs and property values are passed as-is to the SAP COM layer. Validate inputs in your own automation code.
- **Scripting must be enabled**: SAP GUI scripting must be explicitly enabled both server-side and client-side. This library cannot enable it programmatically.
- **Credential handling**: this wrapper does not manage SAP credentials; use the SAP GUI logon dialog or SNC/SSO as normal.
