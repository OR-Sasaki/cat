---
name: kiro-impl-pr
description: cc-sdd（Kiro仕様駆動開発）のタスクを実装し、GitHubにPRを作成する。`/kiro:spec-impl`の拡張版で、ブランチ作成、小項目ごとのコミット、push、PR作成を自動化する。使用タイミング：cc-sddでタスク定義が完了し、タスクを実装してPRを作成したいとき。
argument-hint: <feature-name> <task-number>
---

# Implementation with PR Creation

cc-sddタスクを実装し、GitHubにPRを作成するワークフロー。

## Execution Flow

1. **Load Context** - spec files and steering documents
2. **Determine Branches** - task branch and base branch from tasks.md
3. **Create Branch** - checkout task branch
4. **Implement** - execute task with commits per sub-task
5. **Update tasks.md** - mark task as completed
6. **Push & Create PR** - push and create PR when all tasks in branch are completed
7. **Review Response** - address PR review feedback (after review)

## Tool Guidance

- **Read first**: Load all context before implementation
- **Test first**: Write tests before code (when applicable)
- Use **WebSearch/WebFetch** for library documentation when needed

---

## Step 1: Load Context

Read all necessary context:
- `.kiro/specs/$1/spec.json`, `requirements.md`, `design.md`, `tasks.md`
- Entire `.kiro/steering/` directory

Validate:
- Verify tasks are approved in spec.json
- If not approved, stop and suggest: "Complete previous phases first"

## Step 2: Determine Branches

### Branch Structure in tasks.md

tasks.md uses section headers to define branches:

```markdown
## Branches

**Base**: `feature/shop-scene-mock`

| Branch | Tasks | Goal |
|--------|-------|------|
| `feature/shop-scene-mock-ui` | 1-6 | UI implementation |
| `feature/shop-scene-mock-logic` | 7-9 | Logic implementation |

## Tasks

### Branch: `feature/shop-scene-mock-ui`

- [ ] 1. First task
  - [ ] 1.1 Sub-task
  ...
```

### Task Branch
Find which `### Branch:` section contains task `$2` and extract the branch name.

Example: If `$2` is "1" or "1.1", and task 1 is under `### Branch: \`feature/shop-scene-mock-ui\``, then task branch = `feature/shop-scene-mock-ui`

### Base Branch
Extract from the `**Base**:` line in tasks.md.

Example: `**Base**: \`feature/shop-scene-mock\`` → base branch = `feature/shop-scene-mock`

### PR Target Branch
- Task branches PR to: **base branch** (e.g., `feature/shop-scene-mock`)
- Base branch PRs to: `main` (when all task branches are merged)

## Step 3: Create Branch

```bash
git fetch origin
git checkout -b {task-branch} origin/{base-branch}
```

If base branch doesn't exist on remote yet:
```bash
git checkout -b {base-branch} origin/main
git push -u origin {base-branch}
git checkout -b {task-branch} {base-branch}
```

If task branch already exists locally:
```bash
git checkout {task-branch}
git pull origin {base-branch}
```

## Step 4: Implement Task

For the specified task in `$2`, implement following TDD methodology when appropriate.

### TDD Cycle (Recommended, not mandatory)
1. **RED** - Write failing test
2. **GREEN** - Write minimal code to pass
3. **REFACTOR** - Clean up
4. **VERIFY** - All tests pass

### Commit per Sub-task
After completing each sub-task (1.1, 1.2, etc.), create a commit.

**Important**: Only add files that were modified/created for this sub-task. Do NOT use `git add -A` as it may include unrelated changes by others.

```bash
git add <file1> <file2> ...
git commit -m "Prepare scene constants and file structure"
```

### Commit Message Format
- Language: English
- First letter: Uppercase
- No prefix (no "feat:", "fix:", etc.)
- Concise, describe the sub-task completed

Examples:
- `Prepare scene constants and file structure`
- `Set up VContainer DI container`
- `Configure entry point`

### Critical Constraints
- **Task Scope**: Implement only what the specific task requires
- **Design Alignment**: Implementation must follow design.md specifications
- **No Over-engineering**: Avoid adding features beyond requirements
- **TDD When Applicable**: Tests recommended but not mandatory (especially for Unity)

## Step 5: Update tasks.md

After task completion, update checkboxes for both major task and sub-tasks:
```
- [ ] 1. task name  →  - [x] 1. task name
  - [ ] 1.1 sub-task  →  - [x] 1.1 sub-task
  - [ ] 1.2 sub-task  →  - [x] 1.2 sub-task
```

Commit this change:
```bash
git add .kiro/specs/$1/tasks.md
git commit -m "Mark task $2 as completed"
```

## Step 6: Push & Create PR (When Branch Completed)

### Check if All Tasks in Branch are Completed

Parse tasks.md to check if all major tasks in the current branch section are marked as `[x]`.

Example: If branch `feature/shop-scene-mock-ui` contains tasks 1-6:
- Check if tasks 1, 2, 3, 4, 5, 6 all have `- [x]`
- If yes → Push & Create PR
- If no → Skip, output "Remaining tasks: X, Y, Z"

### Push & Create PR (Only when all tasks completed)

**Push:**
```bash
git push -u origin {task-branch}
```

**Title Format:** Japanese, branch goal from tasks.md table
Example: `画面表示、タブ切り替え、戻るボタンが動作する状態`

**Get Repository URL:**
```bash
gh repo view --json url -q .url
```

**Body Format:**
```markdown
## ブランチ
- Feature: {feature-name}
- Branch: {task-branch}
- Tasks: {task-range} (e.g., 1-6)

## 実装内容
- [x] 1. {task-1-title}
- [x] 2. {task-2-title}
- ...

## 関連ドキュメント
- [Requirements]({repo-url}/blob/{task-branch}/.kiro/specs/{feature-name}/requirements.md)
- [Design]({repo-url}/blob/{task-branch}/.kiro/specs/{feature-name}/design.md)
- [Tasks]({repo-url}/blob/{task-branch}/.kiro/specs/{feature-name}/tasks.md)
```

**Command:**
```bash
gh pr create --base {base-branch} --head {task-branch} --title "{title}" --body "{body}"
```

## Step 7: Review Response (After PR Review)

Guidelines for addressing PR review feedback.

### Commit Strategy
- **Do NOT use `git commit --amend`** for review fixes
- **Create new commits** for each fix so reviewers can see what changed
- Group related fixes into logical commits (e.g., one commit per review comment)

### Commit Message Format
Review fix commits should clearly describe what was fixed:
- `Remove redundant active dialog tracking`
- `Add stack trace to error log for prefab load failure`
- `Move DialogContainer from View to Service`

### Push
```bash
git push origin {task-branch}
```

### Reply to Review
After pushing fixes, reply to the review comment summarizing:
1. What was fixed
2. How it was fixed
3. Any additional improvements made

---

## Output Description

Provide brief summary in the language specified in spec.json:

**When PR created:**
1. **Branch**: Task branch name
2. **Commits**: Number of commits made
3. **PR**: PR URL
4. **Status**: "All tasks in branch completed"

**When PR not created (remaining tasks):**
1. **Branch**: Task branch name
2. **Commits**: Number of commits made (local only)
3. **Remaining**: List of uncompleted tasks in branch
4. **Next**: Suggest next task to implement

**Format**: Concise (under 150 words)

## Error Handling

### Tasks Not Approved
- Stop execution
- Suggest: `/kiro:spec-requirements`, `/kiro:spec-design`, `/kiro:spec-tasks`

### Branch Conflict
- If task branch already exists with different base, warn user
- Suggest: manual resolution or force reset

### PR Creation Failure
- Show error message
- Suggest: manual PR creation with `gh pr create`
