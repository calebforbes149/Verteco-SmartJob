# SmartJob iOS Build and Release Guide

## Overview

The SmartJob mobile application is built using .NET MAUI and supports Android and iOS.

Android builds can be created on Windows, but iOS builds require access to macOS because Apple requires Xcode and Apple signing tools.

This document explains the steps required to build, sign and release the SmartJob application for iOS.

## Requirements

The following are required for an iOS build:

- A Mac running a supported version of macOS
- Xcode installed
- .NET 10 SDK
- .NET MAUI workload
- Apple Developer account
- Apple development or distribution certificate
- iOS provisioning profile
- Access to the SmartJob GitHub repository

The MAUI iOS target used by the project is:

```text
net10.0-ios