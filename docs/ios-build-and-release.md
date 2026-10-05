# SmartJob iOS Build and Release Guide

## Overview

SmartJob is a .NET MAUI mobile application supporting Android and iOS.

Android development and release builds can be completed on Windows. Building and signing the iOS version requires macOS because Apple requires Xcode and Apple code-signing tools.

This document explains the required process for building, signing and releasing SmartJob on iOS.

## Project Details

Mobile project:

`SmartJobMobile/SmartJobMobile/SmartJobMobile.csproj`

iOS target framework:

`net10.0-ios`

Application identifier:

`za.co.verteco.smartjob`

Production API:

`https://verteco-smartjob-api-hcdyfgf4cagdh2db.southafricanorth-01.azurewebsites.net/`

## Requirements

A developer producing an iOS build requires:

- A compatible Mac running macOS
- Xcode
- .NET 10 SDK
- .NET MAUI iOS workload
- Access to the SmartJob GitHub repository
- Apple Developer account
- Apple signing certificate
- iOS provisioning profile

## Preparing the Mac

Install the MAUI iOS workload:

```bash
dotnet workload install maui-ios

```

Confirm the .NET installation:

```bash
dotnet --info
```

Xcode must be installed and opened at least once so that its licence can be accepted and the required Apple components can be installed.

## Clone the Repository

On the Mac, clone the SmartJob repository:

```bash
git clone https://github.com/EMGPRS/insy7315-2026-task-2-st10453511.git
```

Move into the repository:

```bash
cd insy7315-2026-task-2-st10453511
```

The final approved application should be built from the team's stable `main` branch.

## Restore the Project

Restore the required packages:

```bash
dotnet restore ./SmartJobMobile/SmartJobMobile/SmartJobMobile.csproj
```

## Debug Build

A development iOS build can be created using:

```bash
dotnet build ./SmartJobMobile/SmartJobMobile/SmartJobMobile.csproj \
  -f net10.0-ios \
  -c Debug
```

The application can then be tested using an iOS simulator on macOS.

Running SmartJob on a physical iPhone requires valid Apple signing and a provisioning profile.

## Apple Signing

To install SmartJob on a physical iOS device or produce a production release, the following must be configured:

1. Register an App ID in the Apple Developer portal.
2. Use the SmartJob bundle identifier `za.co.verteco.smartjob`.
3. Create or use a valid Apple Development certificate.
4. Create or use an Apple Distribution certificate for production release.
5. Create the required iOS provisioning profile.
6. Install the signing certificate in the macOS Keychain.

Certificates, private keys, provisioning profiles and signing passwords must never be committed to GitHub.

## Release Build

Once Apple signing has been configured, a Release build can be created using:

```bash
dotnet publish ./SmartJobMobile/SmartJobMobile/SmartJobMobile.csproj \
  -f net10.0-ios \
  -c Release
```

Additional signing settings can be supplied through the project configuration or secure CI/CD environment variables.

## Production API

The SmartJob production mobile application connects to:

`https://verteco-smartjob-api-hcdyfgf4cagdh2db.southafricanorth-01.azurewebsites.net/`

Production communication uses HTTPS.

The final iOS application must not use localhost, Android emulator addresses or other machine-specific development URLs.

## Authentication and Security

The iOS application uses the same SmartJob authentication architecture as Android.

This includes:

- Technician authentication through the SmartJob API
- JWT access tokens
- Refresh tokens
- Refresh-token rotation
- Automatic access-token refresh
- Secure token storage
- Expired-session handling
- Revoked-session handling
- Secure logout
- Server-side refresh-token revocation

Authentication information is stored using .NET MAUI SecureStorage.

Sensitive credentials must not be hard-coded into the application or committed to source control.

## App Store Distribution

For production release through the App Store:

1. Create the SmartJob application in App Store Connect.
2. Configure the SmartJob bundle identifier.
3. Configure an Apple Distribution certificate.
4. Configure an App Store provisioning profile.
5. Produce the signed Release build.
6. Archive the application.
7. Upload the application using Xcode or another Apple-supported upload tool.
8. Complete the required App Store metadata and privacy information.
9. Submit the application for Apple review.

## CI/CD Considerations

Automated iOS builds require a macOS GitHub Actions runner.

The Linux runner used by the current SmartJob Android pipeline cannot compile and sign an iOS application.

Apple certificates, provisioning profiles and passwords should be stored securely using GitHub Secrets or another approved secret-management platform.

The following must never be committed to the repository:

- Apple signing certificates
- Provisioning profiles
- Private keys
- Apple Developer passwords
- App-specific passwords
- Production secrets

## Current Project Position

The SmartJob Android release process has been implemented and tested, including signed APK generation.

The iOS application shares the same .NET MAUI project, authentication services and hosted Azure API.

A physical iOS Release build was not produced during development because the available development environment was Windows-based.

Apple requires access to macOS, Xcode and valid Apple Developer signing resources for the final iOS build.

This document provides the required handover process for a developer with access to those resources to build, sign and release the SmartJob iOS application.