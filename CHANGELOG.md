# Changelog

All notable changes to Structopedia will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Project scaffolding: solution, shared build properties (net10.0, nullable, warnings as errors), StyleCop and .NET analyzers, and a `PackMod` target that writes the Mod DB zip to `release/`.
- Client-only mod system skeleton (`StructopediaModSystem`), which loads on the client side only and logs the mod name and version at startup.
- CI on GitHub Actions: build and unit tests against a cached Vintage Story 1.22.6 server install, on every push and pull request.
