# Implementation Plan

## Branch Format Template

**Multiple Tasks = 1 Branch (1 PR)**: Tasks are grouped into branches. Each branch represents one PR that delivers a working, testable state.

### Branch hierarchy

**Small features (base branch only)**:
```
feature/{{FEATURE_NAME}}  ← Single branch for all tasks (PR directly to main)
```

**Large features (base + task branches)**:
```
feature/{{FEATURE_NAME}}           ← Base branch (created from main)
├── feature/{{FEATURE_NAME}}-{{BRANCH_1}}  ← Task branch (PR to base)
└── feature/{{FEATURE_NAME}}-{{BRANCH_2}}  ← Task branch (PR to base)
```

Use base branch only when the feature is small enough to review in a single PR. Split into task branches when the feature requires multiple reviewable units.

### Branches section (base only)
```markdown
## Branches

**Base**: `feature/{{FEATURE_NAME}}`

All tasks are implemented in the base branch.
```

### Branches section (with task branches)
```markdown
## Branches

**Base**: `feature/{{FEATURE_NAME}}`

| Branch | Tasks | Goal |
|--------|-------|------|
| `feature/{{FEATURE_NAME}}-{{BRANCH_NAME}}` | 1-N | {{WORKING_STATE_DESCRIPTION}} |
| `feature/{{FEATURE_NAME}}-{{BRANCH_NAME2}}` | N+1-M | {{WORKING_STATE_DESCRIPTION}} |
```

### Tasks section (with sub-tasks)
```markdown
## Tasks

### Branch: `feature/{{FEATURE_NAME}}-{{BRANCH_NAME}}`

- [ ] 1. {{MAJOR_TASK_DESCRIPTION}}
  - [ ] 1.1 {{SUBTASK_DESCRIPTION}}
    - {{DETAIL_ITEM_1}}
    - {{DETAIL_ITEM_2}}
  - [ ] 1.2 {{SUBTASK_DESCRIPTION}}
    - {{DETAIL_ITEM_1}}
  - _Requirements: {{REQUIREMENT_IDS}}_ *(IDs only; do not add descriptions or parentheses.)*

- [ ] 2. {{MAJOR_TASK_DESCRIPTION}}
  - [ ] 2.1 {{SUBTASK_DESCRIPTION}}
    - {{DETAIL_ITEM_1}}
  - [ ] 2.2 {{SUBTASK_DESCRIPTION}}
    - {{DETAIL_ITEM_1}}
  - _Requirements: {{REQUIREMENT_IDS}}_
```

### Simple format (no sub-tasks needed)
```markdown
- [ ] 1. {{TASK_DESCRIPTION}}
  - {{DETAIL_ITEM_1}}
  - {{DETAIL_ITEM_2}}
  - _Requirements: {{REQUIREMENT_IDS}}_
```

## Task Format Rules

- Major tasks within a branch MUST be sequential (no gaps: 1,2,3 OK; 1,3,5 NG)
- Sub-tasks use `N.M` format where N is the major task number (1.1, 1.2, 2.1, 2.2...)
- Sub-tasks have their own checkboxes for progress tracking
- Do NOT include `_Branch:` line in individual tasks (branch is defined by section header)
- End each major task with `_Requirements: X.X, Y.Y_` (IDs only)

## Branch Workflow

**Base branch only**:
1. Create base branch from `main`: `feature/{{FEATURE_NAME}}`
2. Implement all tasks in base branch
3. PR base branch to `main`

**With task branches**:
1. Create base branch from `main`: `feature/{{FEATURE_NAME}}`
2. Create task branches from base: `feature/{{FEATURE_NAME}}-{{BRANCH_NAME}}`
3. PR each task branch to base branch
4. When all task branches merged, PR base branch to `main`

> **Optional test coverage**: When a task is deferrable test work tied to acceptance criteria, mark the checkbox as `- [ ]*` and explain the referenced requirements in the detail bullets.
> 
