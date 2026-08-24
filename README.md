# prKanban

This project is a Firebase-hosted Kanban board with task persistence in Firestore.

## Local setup

1. Install Firebase CLI if needed.
2. Log in with Firebase:
   `npx -y firebase-tools@latest login`
3. In the project root, run:
   `npx -y firebase-tools@latest init firestore`
4. Use the included `firestore.rules` file.
5. Deploy:
   `npx -y firebase-tools@latest deploy --only hosting,firestore`

## Important

The app writes task documents to the Firestore collection `tasks`.
If Firestore cannot be reached, the app falls back to `localStorage` as a temporary safety net only.

## Data model

Each task document is stored under `tasks/{taskId}` and typically contains fields like:
- title
- taskType
- owner
- customer
- description
- tags
- column
- subColumn
- isArchived
- createdAt
- updatedAt
