# Contributing to Bankweave

Thank you for your interest in contributing to Bankweave! 🎉

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/YourUsername/Bankweave.git`
3. Create a feature branch: `git checkout -b feature/your-feature-name`
4. Start development mode: `docker-compose -f docker-compose.dev.yml up -d`

## Development Workflow

1. Make your changes
2. Test thoroughly with hot reload active
3. Commit with clear messages: `git commit -m "feat: add feature description"`
4. Push to your fork: `git push origin feature/your-feature-name`
5. Open a Pull Request

## Code Style

- Follow C# conventions (PascalCase for public members, camelCase for private)
- Keep API endpoints RESTful
- Document new features in README.md
- Add CSV provider documentation to CSV_IMPORT_GUIDE.md if adding new bank support

## Adding New Bank Support

1. Add parser logic in `Controllers/CsvImportController.cs`
2. Test with real CSV file from that bank
3. Document CSV format in `CSV_IMPORT_GUIDE.md`
4. Update README.md supported banks list

## Testing

- Test with real CSV files from multiple banks
- Verify authentication flow works
- Check responsive design on mobile
- Ensure Docker builds complete successfully

## Pull Request Guidelines

- Describe what your PR does
- Link any related issues
- Include screenshots for UI changes
- Ensure all Docker containers build and run

## Questions?

Open an issue for discussion before starting major changes.

Thank you for contributing! 💙
