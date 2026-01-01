---
name: kiro-impl-pr
description: cc-sdd（Kiro仕様駆動開発）のタスクを実装し、GitHubにPRを作成する。`/kiro:spec-impl`の拡張版で、ブランチ作成、小項目ごとのコミット、push、PR作成を自動化する。使用タイミング：cc-sddでタスク定義が完了し、タスクを実装してPRを作成したいとき。
argument-hint: <feature-name> <task-number>
---

# Implementation with PR Creation

cc-sddタスクを実装し、GitHubにPRを作成するワークフロー。

## Execution Flow

1. **Load Context** - spec files and steering documents
2. **Determine Branches** - task branch and base branch
3. **Create Branch** - checkout task branch
4. **Implement** - execute task with commits per sub-item
5. **Update tasks.md** - mark task as completed
6. **Push & Create PR** - push and create GitHub PR

## Tool Guidance

- **Read first**: Load all context before implementation
- **Test first**: Write tests before code (when applicable)
- Use **WebSearch/WebFetch** for library documentation when needed

## Step 1: Load Context

Read all necessary context:
- `.kiro/specs/$1/spec.json`, `requirements.md`, `design.md`, `tasks.md`
- Entire `.kiro/steering/` directory

Validate:
- Verify tasks are approved in spec.json
- If not approved, stop and suggest: "Complete previous phases first"

## Step 2: Determine Branches

### Task Branch
Extract from `tasks.md` the `_Branch:` value for task `$2`.

Example in tasks.md:
```
- [ ] 1. record型有効化とダイアログ共通型の定義
  - ...
  - _Branch: `feature/dialog-system-foundation-types`_
```

### Base Branch
Determine base branch with this logic:

1. Get the previous task number (e.g., if $2 is "2.1", previous is "1.1")
2. Check if previous task's branch has an open PR:
   ```bash
   gh pr list --head {previous-branch} --state open
   ```
3. If open PR exists → base = previous task's branch
4. If no open PR → base = feature branch (e.g., `feature/dialog-system`)

For the first task (e.g., "1.1"), base = feature branch.

## Step 3: Create Branch

```bash
git fetch origin
git checkout -b {task-branch} origin/{base-branch}
```

If branch already exists locally:
```bash
git checkout {task-branch}
git pull origin {base-branch}
```

## Step 4: Implement Task

For the specified task in `$2`, implement following TDD methodology when appropriate:

### TDD Cycle (Recommended, not mandatory)
1. **RED** - Write failing test
2. **GREEN** - Write minimal code to pass
3. **REFACTOR** - Clean up
4. **VERIFY** - All tests pass

### Commit per Sub-item
After completing each sub-item (bullet point in task), create a commit.

**Important**: Only add files that were modified/created for this sub-item. Do NOT use `git add -A` as it may include unrelated changes by others.

```bash
git add <file1> <file2> ...
git commit -m "Add UniTask package"
```

**Commit Message Format:**
- Language: English
- First letter: Uppercase
- No prefix (no "feat:", "fix:", etc.)
- No description body
- Concise, state the purpose

Examples:
- `Add UniTask package`
- `Define dialog result enum`
- `Implement stack-based dialog state`

### Critical Constraints
- **Task Scope**: Implement only what the specific task requires
- **Design Alignment**: Implementation must follow design.md specifications
- **No Over-engineering**: Avoid adding features beyond requirements
- **TDD When Applicable**: Tests recommended but not mandatory (especially for Unity)

## Step 5: Update tasks.md

After task completion, update checkbox:
```
- [ ] 1.1 task name  →  - [x] 1.1 task name
```

Commit this change:
```bash
git add .kiro/specs/$1/tasks.md
git commit -m "Mark task $2 as completed"
```

## Step 6: Push & Create PR

### Push
```bash
git push -u origin {task-branch}
```

### Create PR

**Title Format:** Japanese, task name (without task number)
Example: `record型有効化とダイアログ共通型の定義`

**Get Repository URL:**
```bash
gh repo view --json url -q .url
# Example output: https://github.com/owner/repo
```

**Body Format:**
```markdown
## タスク
- Feature: {feature-name}
- Task: {task-number} {task-title}

## 実装内容
- {sub-item 1}
- {sub-item 2}
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

## Output Description

Provide brief summary in the language specified in spec.json:

1. **Branch**: Created branch name
2. **Commits**: Number of commits made
3. **PR**: PR URL
4. **Status**: Next task suggestion

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
