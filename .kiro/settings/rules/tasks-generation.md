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

**Branch Independence Requirement**:
- Each branch (PR) must be self-contained and compile without errors on its own.
- Each branch must deliver a working, testable state (e.g., "UI displays and tabs work" or "purchase flow completes").
- Tasks within a branch build on each other sequentially.

**End with integration tasks** to wire everything together.

### 3. Flexible Task Sizing

**Guidelines**:
- **Major tasks**: Group related work by cohesion
- **Sub-tasks**: Break down major tasks into trackable units (1-3 hours each)
- **Task size**: Each major task should take 2-8 hours total
- Balance between too granular (single file) and too broad (entire feature)

**Don't force arbitrary numbers** - let logical grouping determine structure.

### 4. Requirements Mapping

**End each major task detail section with**:
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

## Branch Grouping Rules

### Multiple Tasks = 1 Branch (1 PR)

Tasks are grouped into branches. Each branch represents one PR that delivers a working, testable state.

**Branch Naming**:
- Format: `feature/{FEATURE_NAME}-{BRANCH_NAME}`
- Example: `feature/shop-scene-mock-ui`, `feature/shop-scene-mock-logic`

### Branch Grouping Criteria

**Each branch must**:
1. **Deliver a working state**: The code must be runnable and testable after the branch is merged (e.g., "UI displays correctly", "basic operations work").
2. **Contain sequential tasks only**: Major tasks within a branch MUST be numbered consecutively with no gaps.
   - ✅ OK: Branch 1 = Tasks 1,2,3; Branch 2 = Tasks 4,5
   - ❌ NG: Branch 1 = Tasks 1,2,4; Branch 2 = Tasks 3,5 (gaps/interleaving)
3. **Group by functional area**: Separate branches by major concerns:
   - UI/View layer vs Business logic layer
   - Independent features that don't share dependencies
   - Foundation/setup vs Feature implementation

**Branch splitting guidelines**:
- **UI vs Logic**: Separate display/interaction from business processing when possible
- **Large logic**: If logic is complex (multiple script files for one feature), split by independent functional units
- **Dependencies**: Tasks that share state or call each other belong in the same branch
- **Testability**: Each branch should be demonstrable to stakeholders

## Task Hierarchy Rules

### Major Tasks and Sub-tasks

**Major tasks** (1, 2, 3...):
- Represent a cohesive functional unit
- Increment sequentially across all branches (never repeat)
- Each major task ends with `_Requirements: X.X, Y.Y_`

**Sub-tasks** (1.1, 1.2, 2.1, 2.2...):
- Break down major tasks into trackable units
- Each sub-task has its own checkbox for progress tracking
- Sub-task numbering: `{MajorTask}.{SubTask}` (e.g., 1.1, 1.2, 2.1)
- Use when a major task has multiple distinct steps worth tracking separately

### Task Format

**With sub-tasks**:
```markdown
### Branch: `feature/{feature-name}-{branch-name}`

- [ ] 1. First major task description
  - [ ] 1.1 First sub-task description
    - Detail item 1
    - Detail item 2
  - [ ] 1.2 Second sub-task description
    - Detail item 1
  - _Requirements: X.X, Y.Y_

- [ ] 2. Second major task description
  - [ ] 2.1 First sub-task description
    - Detail item 1
  - [ ] 2.2 Second sub-task description
    - Detail item 1
  - _Requirements: Z.Z, W.W_

### Branch: `feature/{feature-name}-{branch-name2}`

- [ ] 3. Third major task (continues numbering)
  - [ ] 3.1 Sub-task description
    - ...
```

**Without sub-tasks (simple tasks)**:
```markdown
- [ ] 1. Simple task description
  - Detail item 1
  - Detail item 2
  - _Requirements: X.X, Y.Y_
```

**Do NOT include `_Branch:` line in individual tasks** - the branch is defined by the section header.

## Requirements Coverage

**Mandatory Check**:
- ALL requirements from requirements.md MUST be covered
- Cross-reference every requirement ID with task mappings
- If gaps found: Return to requirements or design phase
- No requirement should be left without corresponding tasks

Use `N.M`-style numeric requirement IDs where `N` is the top-level requirement number from requirements.md (for example, Requirement 1 → 1.1, 1.2; Requirement 2 → 2.1, 2.2), and `M` is a local index within that requirement group.

Document any intentionally deferred requirements with rationale.
