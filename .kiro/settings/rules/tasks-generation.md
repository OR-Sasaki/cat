# Task Generation Rules

## Core Principles

### 1. Natural Language Descriptions
Focus on capabilities and outcomes, not code structure.

**Describe**:
- What functionality to achieve
- Business logic and behavior
- Features and capabilities
- Domain language and concepts
- Data relationships and workflows

**Avoid**:
- File paths and directory structure
- Function/method names and signatures
- Type definitions and interfaces
- Class names and API contracts
- Specific data structures

**Rationale**: Implementation details (files, methods, types) are defined in design.md. Tasks describe the functional work to be done.

### 2. Task Integration & Progression

**Every task must**:
- Build on previous outputs (no orphaned code)
- Connect to the overall system (no hanging features)
- Progress incrementally (no big jumps in complexity)
- Validate core functionality early in sequence
- Respect architecture boundaries defined in design.md (Architecture Pattern & Boundary Map)
- Honor interface contracts documented in design.md
- Use task descriptions sparingly—detailed work items go in nested bullet points.

**PR Independence Requirement**:
- Each PR must be self-contained and compile without errors on its own.
- Task N must NOT reference definitions that will be created in Task N+1.
- If Task 1 needs something from Task 2, reorder the tasks or merge them.

**End with integration tasks** to wire everything together.

### 3. Flexible Task Sizing

**Guidelines**:
- **Major tasks**: Group related work by cohesion (1 task = 1 PR)
- **Task size**: Each task should take 2-8 hours, containing 3-15 detail items
- Balance between too granular (single file) and too broad (entire feature)

**Don't force arbitrary numbers** - let logical grouping determine structure.

### 4. Requirements Mapping

**End each task detail section with**:
- `_Requirements: X.X, Y.Y_` listing **only numeric requirement IDs** (comma-separated). Never append descriptive text, parentheses, translations, or free-form labels.
- For cross-cutting requirements, list every relevant requirement ID. All requirements MUST have numeric IDs in requirements.md. If an ID is missing, stop and correct requirements.md before generating tasks.
- Reference components/interfaces from design.md when helpful (e.g., `_Contracts: AuthService API`)

### 5. Code-Only Focus

**Include ONLY**:
- Coding tasks (implementation)
- Testing tasks (unit, integration, E2E)
- Technical setup tasks (infrastructure, configuration)

**Exclude**:
- Deployment tasks
- Documentation tasks
- User testing
- Marketing/business activities

### Optional Test Coverage Tasks

- When the design already guarantees functional coverage and rapid MVP delivery is prioritized, mark purely test-oriented follow-up work (e.g., baseline rendering/unit tests) as **optional** using the `- [ ]*` checkbox form.
- Only apply the optional marker when the task directly references acceptance criteria from requirements.md in its detail bullets.
- Never mark implementation work or integration-critical verification as optional—reserve `*` for auxiliary/deferrable test coverage that can be revisited post-MVP.

## Task Hierarchy Rules

### 1 Task = 1 PR (No Sub-tasks)
- **Only major tasks** (1, 2, 3, 4...) are used. Do NOT create sub-tasks (1.1, 1.2).
- Each major task represents one PR-sized unit of work.
- Group related work into a single major task using nested bullet points and bold subtopics.
- Major tasks MUST increment sequentially: 1, 2, 3, 4, 5... (never repeat numbers).

**Branch Naming**:
- Feature branch: `feature/{FEATURE_NAME}`
- Task branch: `feature/{FEATURE_NAME}-{TASK_NAME}`

### Task Sizing
- Each task should be a cohesive functional unit (e.g., "Dialog State Management", "Dialog UI Components").
- Avoid tasks that are too small (single file/definition only) or too large (entire feature).
- GOOD: Setup tasks, single responsibility + its dependencies, grouped related components.
- BAD: Single file changes, overly broad "implement everything" tasks.

### Parallel Analysis (default)
- Assume parallel analysis is enabled unless explicitly disabled (e.g. `--sequential` flag).
- Identify tasks that can run concurrently when **all** conditions hold:
  - No data dependency on other pending tasks
  - No shared file or resource contention
  - No prerequisite review/approval from another task
- Validate that identified parallel tasks operate within separate boundaries defined in the Architecture Pattern & Boundary Map.
- Confirm API/event contracts from design.md do not overlap in ways that cause conflicts.
- Append `(P)` immediately after the task number for each parallel-capable task:
  - Example: `- [ ] 2. (P) Build background worker`
- If sequential mode is requested, omit `(P)` markers entirely.
- Explicitly call out dependencies that prevent `(P)` even when tasks look similar.

### Checkbox Format
```markdown
- [ ] 1. First major task description
  - **Subtopic A**
    - Detail item 1
    - Detail item 2
  - **Subtopic B**
    - Detail item 3
  - _Requirements: X.X, Y.Y_
  - _Branch: `feature/feature-name-task-name`_

- [ ] 2. Second major task description
  - Detail item 1
  - Detail item 2
  - _Requirements: Z.Z, W.W_
  - _Branch: `feature/feature-name-task-name`_

- [ ] 3. Third major task (NOT 1 or 2 again!)
  - ...
```

## Requirements Coverage

**Mandatory Check**:
- ALL requirements from requirements.md MUST be covered
- Cross-reference every requirement ID with task mappings
- If gaps found: Return to requirements or design phase
- No requirement should be left without corresponding tasks

Use `N.M`-style numeric requirement IDs where `N` is the top-level requirement number from requirements.md (for example, Requirement 1 → 1.1, 1.2; Requirement 2 → 2.1, 2.2), and `M` is a local index within that requirement group.

Document any intentionally deferred requirements with rationale.
