#!/bin/bash

# Script to create and push a git tag
# Usage: ./tag.sh 1.0.0 "Release message"

set -e

if [ -z "$1" ]; then
    echo "Usage: ./tag.sh <version> [message]"
    echo "Example: ./tag.sh 1.0.0 \"Initial release\""
    exit 1
fi

VERSION="v$1"
MESSAGE="${2:-Release $VERSION}"

# Check if tag already exists
if git rev-parse "$VERSION" >/dev/null 2>&1; then
    echo "Error: Tag $VERSION already exists"
    echo "To delete it locally: git tag -d $VERSION"
    echo "To delete it remotely: git push origin :refs/tags/$VERSION"
    exit 1
fi

echo "Creating tag: $VERSION"
echo "Message: $MESSAGE"
echo ""

# Create annotated tag
git tag -a "$VERSION" -m "$MESSAGE"

echo "Tag created successfully!"
echo "Pushing to origin..."

git push origin "$VERSION"

echo "Tag pushed successfully!"
