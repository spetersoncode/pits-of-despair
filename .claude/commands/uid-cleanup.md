Manage .uid files in the Scripts/ folder and all subfolders:
1. Find orphaned .uid files (those without corresponding script files), present findings, delete them, and commit if any were found
2. Find any uncommitted or untracked .uid files, add them to git, and commit with an appropriate conventional commit message

If both types of changes are found, create separate commits for each.
