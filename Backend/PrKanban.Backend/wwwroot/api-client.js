/**
 * prKanban REST API Client
 * Replaces Firebase SDK with ASP.NET Core REST API
 * 
 * Usage:
 *   - AuthAPI.login(email, password)
 *   - TaskAPI.getTasks()
 *   - TaskAPI.createTask(data)
 */

const API_BASE = 'http://localhost:5000/api/v1';

/**
 * Authentication API
 * Handles user registration, login, logout via REST
 */
const AuthAPI = {
  /**
   * Register new user
   */
  async register(email, password, fullName) {
    try {
      const response = await fetch(`${API_BASE}/auth/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include', // Important: Send/receive httpOnly cookies
        body: JSON.stringify({
          email,
          password,
          confirmPassword: password, // API requires confirmation
          fullName: fullName || email.split('@')[0]
        })
      });

      const data = await response.json();
      
      if (!response.ok) {
        const errorMessage = data.error?.message || data.error || 'Registration failed';
        throw new Error(errorMessage);
      }

      return data.data || data;
    } catch (error) {
      console.error('Registration error:', error);
      throw error;
    }
  },

  /**
   * Login with email and password
   */
  async login(email, password) {
    try {
      const response = await fetch(`${API_BASE}/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include', // httpOnly cookie will be set by server
        body: JSON.stringify({
          email,
          password
        })
      });

      const data = await response.json();

      if (!response.ok) {
        const errorMessage = data.error?.message || data.error || 'Login failed';
        const error = new Error(errorMessage);
        error.code = data.error?.code;
        throw error;
      }

      return data.data || data;
    } catch (error) {
      console.error('Login error:', error);
      throw error;
    }
  },

  /**
   * Logout current user
   */
  async logout() {
    try {
      const response = await fetch(`${API_BASE}/auth/logout`, {
        method: 'POST',
        credentials: 'include'
      });

      const data = await response.json();

      if (!response.ok) {
        const errorMessage = data.error?.message || data.error || 'Logout failed';
        throw new Error(errorMessage);
      }

      return data.data || data;
    } catch (error) {
      console.error('Logout error:', error);
      throw error;
    }
  },

  /**
   * Get current authenticated user
   */
  async getCurrentUser() {
    try {
      const response = await fetch(`${API_BASE}/auth/user`, {
        credentials: 'include'
      });

      if (!response.ok) {
        return null; // Not authenticated
      }

      const data = await response.json();
      return data.data || null;
    } catch (error) {
      console.error('Get current user error:', error);
      return null;
    }
  }
};

/**
 * Task API
 * Handles all task CRUD operations via REST
 */
const TaskAPI = {
  /**
   * Get all tasks with optional filters
   * 
   * Filters:
   *   - column: Filter by column (e.g., 'Todo', 'In Progress')
   *   - archived: true/false
   *   - owner: Filter by assigned person's name
   *   - skip: Pagination offset
   *   - take: Number of tasks to return
   */
  async getTasks(filters = {}) {
    try {
      const params = new URLSearchParams();
      
      if (filters.column) params.append('column', filters.column);
      if (filters.archived !== undefined) params.append('archived', filters.archived);
      if (filters.owner) params.append('owner', filters.owner);
      if (filters.skip) params.append('skip', filters.skip);
      if (filters.take) params.append('take', filters.take);

      const url = params.toString() 
        ? `${API_BASE}/tasks?${params}` 
        : `${API_BASE}/tasks`;

      const response = await fetch(url, {
        credentials: 'include'
      });

      const data = await response.json();

      if (!response.ok) {
        const errorMessage = data.error?.message || data.error || 'Failed to fetch tasks';
        throw new Error(errorMessage);
      }

      return data.data || data;
    } catch (error) {
      console.error('Get tasks error:', error);
      throw error;
    }
  },

  /**
   * Get single task by ID
   */
  async getTask(id) {
    try {
      const response = await fetch(`${API_BASE}/tasks/${id}`, {
        credentials: 'include'
      });

      const data = await response.json();

      if (!response.ok) {
        const errorMessage = data.error?.message || data.error || 'Task not found';
        throw new Error(errorMessage);
      }

      return data.data || data;
    } catch (error) {
      console.error('Get task error:', error);
      throw error;
    }
  },

  /**
   * Create new task
   * 
   * Required fields:
   *   - title: string
   *   - columnName: string (e.g., 'Todo')
   * 
   * Optional fields:
   *   - description: string
   *   - taskType: string (e.g., 'feature', 'bug')
   *   - owner: assigned person's name
   *   - customer: string
   *   - subColumnName: string
   *   - tags: string[]
   */
  async createTask(taskData) {
    try {
      const response = await fetch(`${API_BASE}/tasks`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify(taskData)
      });

      const data = await response.json();

      if (!response.ok) {
        const errorMessage = data.error?.message || data.error || 'Failed to create task';
        throw new Error(errorMessage);
      }

      return data.data || data;
    } catch (error) {
      console.error('Create task error:', error);
      throw error;
    }
  },

  /**
   * Update existing task
   */
  async updateTask(id, updates) {
    try {
      const response = await fetch(`${API_BASE}/tasks/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify(updates)
      });

      const data = await response.json();

      if (!response.ok) {
        const errorMessage = data.error?.message || data.error || 'Failed to update task';
        throw new Error(errorMessage);
      }

      return data.data || data;
    } catch (error) {
      console.error('Update task error:', error);
      throw error;
    }
  },

  async moveTask(id, columnName, subColumnName, beforeTaskId) {
    const response = await fetch(`${API_BASE}/tasks/${encodeURIComponent(id)}/position`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ columnName, subColumnName, beforeTaskId })
    });
    if (!response.ok) {
      throw new Error(`Failed to move task (${response.status})`);
    }
  },

  async getTaskAttachments(taskId) {
    const response = await fetch(`${API_BASE}/tasks/${encodeURIComponent(taskId)}/attachments`, {
      credentials: 'include'
    });
    const data = await response.json();
    if (!response.ok) {
      throw new Error(data.detail || data.error?.message || `Failed to fetch attachments (${response.status})`);
    }
    return data;
  },

  async uploadTaskAttachment(taskId, file, isInlineImage = false) {
    const formData = new FormData();
    formData.append('file', file, file.name || 'pasted-image.png');
    formData.append('isInlineImage', String(isInlineImage));
    const response = await fetch(`${API_BASE}/tasks/${encodeURIComponent(taskId)}/attachments`, {
      method: 'POST',
      credentials: 'include',
      body: formData
    });
    const data = await response.json();
    if (!response.ok) {
      throw new Error(data.title || data.detail || data.error?.message || `Upload failed (${response.status})`);
    }
    return data;
  },

  async deleteTaskAttachment(taskId, attachmentId) {
    const response = await fetch(
      `${API_BASE}/tasks/${encodeURIComponent(taskId)}/attachments/${encodeURIComponent(attachmentId)}`,
      { method: 'DELETE', credentials: 'include' }
    );
    if (!response.ok) {
      const data = await response.json().catch(() => ({}));
      throw new Error(data.detail || data.error?.message || `Delete failed (${response.status})`);
    }
  },

  taskAttachmentContentUrl(taskId, attachmentId) {
    return `${API_BASE}/tasks/${encodeURIComponent(taskId)}/attachments/${encodeURIComponent(attachmentId)}/content`;
  },

  /**
   * Delete task
   */
  async deleteTask(id) {
    try {
      const response = await fetch(`${API_BASE}/tasks/${id}`, {
        method: 'DELETE',
        credentials: 'include'
      });

      const data = await response.json();

      if (!response.ok) {
        const errorMessage = data.error?.message || data.error || 'Failed to delete task';
        throw new Error(errorMessage);
      }

      return data.data || data;
    } catch (error) {
      console.error('Delete task error:', error);
      throw error;
    }
  },

  /**
   * Archive task
   */
  async archiveTask(id) {
    try {
      const response = await fetch(`${API_BASE}/tasks/${id}/archive`, {
        method: 'PUT',
        credentials: 'include'
      });

      const data = await response.json();

      if (!response.ok) {
        const errorMessage = data.error?.message || data.error || 'Failed to archive task';
        throw new Error(errorMessage);
      }

      return data.data || data;
    } catch (error) {
      console.error('Archive task error:', error);
      throw error;
    }
  },

  /**
   * Restore archived task
   */
  async restoreTask(id) {
    try {
      const response = await fetch(`${API_BASE}/tasks/${id}/restore`, {
        method: 'PUT',
        credentials: 'include'
      });

      const data = await response.json();

      if (!response.ok) {
        const errorMessage = data.error?.message || data.error || 'Failed to restore task';
        throw new Error(errorMessage);
      }

      return data.data || data;
    } catch (error) {
      console.error('Restore task error:', error);
      throw error;
    }
  }
};

/**
 * Board Config API
 * Handles board layout and configuration (future enhancement)
 */
const BoardConfigAPI = {
  /**
   * Get board configuration for current user
   */
  async getBoardConfig() {
    try {
      const response = await fetch(`${API_BASE}/board-config`, {
        credentials: 'include'
      });

      if (!response.ok) {
        return null; // Config optional, return null if not found
      }

      const data = await response.json();
      return data.data || null;
    } catch (error) {
      console.error('Get board config error:', error);
      return null; // Fail gracefully
    }
  },

  /**
   * Update board configuration
   */
  async updateBoardConfig(configData) {
    try {
      const response = await fetch(`${API_BASE}/board-config`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify(configData)
      });

      if (!response.ok) {
        console.warn('Failed to update board config, but continuing anyway');
        return { success: false };
      }

      const data = await response.json();
      return { success: true, data: data.data || data };
    } catch (error) {
      console.error('Update board config error:', error);
      return { success: false }; // Fail gracefully
    }
  }
};

/**
 * Export APIs for use in main application
 */
// Make APIs global for browser environment
if (typeof window !== 'undefined') {
  window.AuthAPI = AuthAPI;
  window.TaskAPI = TaskAPI;
  window.BoardConfigAPI = BoardConfigAPI;
}

if (typeof module !== 'undefined' && module.exports) {
  module.exports = { AuthAPI, TaskAPI, BoardConfigAPI };
}
