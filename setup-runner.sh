#!/bin/bash
# Setup script for Bankweave GitHub Actions Self-Hosted Runner
# Run this in WSL to configure a self-hosted runner for automated deployments

set -e

RUNNER_NAME="bankweave-wsl2-runner"
RUNNER_WORK_DIR="_work"
GITHUB_REPO="AmedeoV/Bankweave"

echo "=========================================="
echo "Bankweave Self-Hosted Runner Setup"
echo "=========================================="
echo ""

# Check if running in WSL
if ! grep -q Microsoft /proc/version; then
    echo "⚠️  Warning: This script is designed for WSL2"
    read -p "Continue anyway? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# Create runner directory
RUNNER_DIR="$HOME/actions-runner-bankweave"
echo "📁 Creating runner directory: $RUNNER_DIR"
mkdir -p "$RUNNER_DIR"
cd "$RUNNER_DIR"

# Check if runner already exists
if [ -f "./config.sh" ]; then
    echo "⚠️  Runner already configured in this directory"
    read -p "Do you want to reconfigure? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo "🗑️  Removing existing runner..."
        if [ -f "./svc.sh" ]; then
            sudo ./svc.sh stop || true
            sudo ./svc.sh uninstall || true
        fi
        ./config.sh remove --token "$1" || true
    else
        exit 0
    fi
fi

# Download the latest runner package
echo ""
echo "📥 Downloading GitHub Actions Runner..."
RUNNER_VERSION=$(curl -s https://api.github.com/repos/actions/runner/releases/latest | grep -Po '"tag_name": "v\K[^"]*')
echo "Latest version: $RUNNER_VERSION"

RUNNER_FILE="actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz"
RUNNER_URL="https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/${RUNNER_FILE}"

curl -o "$RUNNER_FILE" -L "$RUNNER_URL"

# Extract the runner
echo "📦 Extracting runner..."
tar xzf "$RUNNER_FILE"
rm "$RUNNER_FILE"

# Configure the runner
echo ""
echo "=========================================="
echo "Runner Configuration"
echo "=========================================="
echo ""
echo "To get your runner token:"
echo "1. Go to: https://github.com/$GITHUB_REPO/settings/actions/runners/new"
echo "2. Copy the token from the configuration command"
echo ""
read -p "Enter your runner registration token: " RUNNER_TOKEN

if [ -z "$RUNNER_TOKEN" ]; then
    echo "❌ Error: Token cannot be empty"
    exit 1
fi

echo ""
echo "🔧 Configuring runner..."
./config.sh \
    --url "https://github.com/$GITHUB_REPO" \
    --token "$RUNNER_TOKEN" \
    --name "$RUNNER_NAME" \
    --work "$RUNNER_WORK_DIR" \
    --labels "wsl,linux,self-hosted,bankweave" \
    --unattended \
    --replace

echo ""
echo "=========================================="
echo "Installing Runner as a Service"
echo "=========================================="
echo ""

# Install and start the runner service
sudo ./svc.sh install
sudo ./svc.sh start

echo ""
echo "✅ Runner setup complete!"
echo ""
echo "Runner Name: $RUNNER_NAME"
echo "Repository: $GITHUB_REPO"
echo "Labels: wsl, linux, self-hosted, bankweave"
echo "Status: Running as a service"
echo ""
echo "=========================================="
echo "Useful Commands:"
echo "=========================================="
echo "Check status:   sudo ./svc.sh status"
echo "Stop service:   sudo ./svc.sh stop"
echo "Start service:  sudo ./svc.sh start"
echo "View logs:      journalctl -u actions.runner.*"
echo ""
echo "Runner location: $RUNNER_DIR"
echo ""
echo "🎉 Your runner is now ready for GitHub Actions workflows!"
