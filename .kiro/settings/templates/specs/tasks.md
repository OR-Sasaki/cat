# Implementation Plan

## Task Format Template

**1 Task = 1 PR**: Each major task corresponds to one PR. Do NOT use sub-tasks (1.1, 1.2). Instead, express details as nested bullet points within the major task.

### Standard format (multiple topics)
- [ ] {{NUMBER}}. {{TASK_DESCRIPTION}}{{PARALLEL_MARK}}
  - **{{SUBTOPIC_1}}**
    - {{DETAIL_ITEM_1}}
    - {{DETAIL_ITEM_2}}
  - **{{SUBTOPIC_2}}**
    - {{DETAIL_ITEM_3}}
    - {{DETAIL_ITEM_4}}
  - _Requirements: {{REQUIREMENT_IDS}}_ *(IDs only; do not add descriptions or parentheses.)*
  - _Branch: `feature/{{FEATURE_NAME}}-{{TASK_NAME}}`_

### Simple format (single topic)
- [ ] {{NUMBER}}. {{TASK_DESCRIPTION}}{{PARALLEL_MARK}}
  - {{DETAIL_ITEM_1}}
  - {{DETAIL_ITEM_2}}
  - _Requirements: {{REQUIREMENT_IDS}}_
  - _Branch: `feature/{{FEATURE_NAME}}-{{TASK_NAME}}`_

> **Parallel marker**: Append ` (P)` only to tasks that can be executed in parallel. Omit the marker when running in `--sequential` mode.
>
> **Optional test coverage**: When a task is deferrable test work tied to acceptance criteria, mark the checkbox as `- [ ]*` and explain the referenced requirements in the detail bullets.
