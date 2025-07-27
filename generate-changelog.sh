#!/bin/bash

# --- CONFIGURATION ---
# The script will try to get the repo URL automatically.
REPO_URL=$(git config --get remote.origin.url | sed 's/\.git$//')
if [ -z "$REPO_URL" ]; then
    echo "Error: Could not determine repository URL."
    echo "Please ensure you are in a git repository with a configured remote 'origin'."
    exit 1
fi

# Get the latest tag as the starting point.
# If no tags exist, it will use the very first commit of the repo.
PREVIOUS_TAG=$(git describe --tags --abbrev=0 2>/dev/null || git rev-list --max-parents=0 HEAD)

# The new version tag is taken from the first argument to the script.
if [ -z "$1" ]; then
  echo "Error: Please provide the new version tag as an argument."
  echo "Example: ./generate-changelog.sh v0.2.0"
  exit 1
fi
NEW_TAG=$1

# The name of the output file.
CHANGELOG_FILE="CHANGELOG.md"

# --- GENERATION ---
echo "Generating changelog from $PREVIOUS_TAG to $NEW_TAG..."

# File Header
echo "# Changelog" > $CHANGELOG_FILE

# Version Header with a link to compare changes
echo "## [$NEW_TAG]($REPO_URL/compare/$PREVIOUS_TAG...$NEW_TAG) ($(date +%Y-%m-%d))" >> $CHANGELOG_FILE
echo "" >> $CHANGELOG_FILE

# Define the sections we want to see in the changelog.
# Format: "commit_type:### Section Header"
SECTIONS=(
  "feat:### ✨ Features"
  "fix:### 🐛 Bug Fixes"
  "docs:### 📚 Documentation"
  "perf:### 🚀 Performance Improvements"
  "refactor:### 🔨 Code Refactoring"
)

# Loop through each section
for section in "${SECTIONS[@]}"; do
  # Split the commit type from the header
  COMMIT_TYPE="${section%%:*}"
  SECTION_HEADER="${section#*:}"

  # Get all commit subjects for the current type within the tag range
  # This temporary file will hold the raw commit messages for this section
  TEMP_COMMITS_FILE=$(mktemp)
  git log $PREVIOUS_TAG..HEAD --grep="^$COMMIT_TYPE" --pretty=format:"%s|%h|%H" > $TEMP_COMMITS_FILE

  # If we found any commits for this section, add them to the file.
  if [ -s $TEMP_COMMITS_FILE ]; then
    echo "$SECTION_HEADER" >> $CHANGELOG_FILE
    echo "" >> $CHANGELOG_FILE
    
    # Process each line of the temporary file to format it correctly.
    while IFS="|" read -r subject shorthash longhash; do
        # Use grep to check if the subject has a scope like (scope).
        # The -q flag makes grep silent.
        if echo "$subject" | grep -qE '^[a-z]+\([^)]+\): '; then
            # If it has a scope, extract scope and message using sed.
            scope=$(echo "$subject" | sed -E 's/^[a-z]+\(([^)]+)\): .*/\1/')
            message=$(echo "$subject" | sed -E 's/^[a-z]+\([^)]+\): (.*)/\1/')
            echo "* **$scope:** $message ([${shorthash}]($REPO_URL/commit/${longhash}))" >> $CHANGELOG_FILE
        else
            # If it does not have a scope, just remove the "type: " prefix.
            message=$(echo "$subject" | sed -E "s/^[a-z]+: //")
            echo "* $message ([${shorthash}]($REPO_URL/commit/${longhash}))" >> $CHANGELOG_FILE
        fi
    done < $TEMP_COMMITS_FILE
    echo "" >> $CHANGELOG_FILE
  fi
  
  # Clean up the temporary file
  rm $TEMP_COMMITS_FILE
done

echo "Done! Changelog saved to $CHANGELOG_FILE"