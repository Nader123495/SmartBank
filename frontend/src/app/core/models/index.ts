// src/app/core/models/index.ts

export interface LoginRequest {
  email: string;
  password: string;
  /** Token reCAPTCHA v2 (si activé). */
  recaptchaToken?: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  /** 3 = Agent, 4 = Client. Optionnel, défaut Agent. */
  roleId?: number;
  professionalId?: string;
  governorate?: string;
  isAgentRequest?: boolean;
  agencyId?: number;
  gender?: string;
  /** Ville de résidence (required for all accounts). */
  city?: string;
}


export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: UserProfile;
}

export interface RegisterResponse {
  requiresVerification: boolean;
  email?: string;
  accessToken?: string;
  refreshToken?: string;
  expiresAt?: string;
  user?: UserProfile;
}

export interface UserProfile {
  id: number;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  role: string;
  agency?: string;
  avatarUrl?: string;
  gender?: string;
  phoneNumber?: string;
  accountNumber?: string;
  permissions: string[];
}


export interface Complaint {
  id: number;
  reference: string;
  title: string;
  description?: string;
  complaintType: string;
  channel: string;
  priority: 'Faible' | 'Moyenne' | 'Haute' | 'Critique';
  status: 'Nouvelle' | 'Assignée' | 'En cours' | 'Validation' | 'Clôturée' | 'Rejetée';
  clientName?: string;
  clientEmail?: string;
  clientPhone?: string;
  clientAccountNumber?: string;
  clientGovernorate?: string;
  submissionCity?: string;
  latitude?: number;
  longitude?: number;
  assignedTo?: string;
  agency?: string;
  isEscalated: boolean;
  isSLABreached: boolean;
  slaDeadline?: string;
  resolutionNote?: string;
  rejectionReason?: string;
  satisfactionRating?: number;
  speedRating?: number;
  qualityRating?: number;
  clientFeedback?: string;
  createdAt: string;
  updatedAt?: string;
  closedAt?: string;

  // Specific fields for dynamic categories
  cardLastFour?: string;
  incidentDate?: string;
  accountType?: string;
  amount?: number;
  virementReference?: string;
  creditType?: string;
  dossierNumber?: string;

  statusHistory?: StatusHistory[];
  comments?: Comment[];
  attachments?: Attachment[];
}

export interface StatusHistory {
  oldStatus?: string;
  newStatus: string;
  changedBy?: string;
  comment?: string;
  changedAt: string;
}

export interface Comment {
  id: number;
  content: string;
  authorName: string;
  isInternal: boolean;
  createdAt: string;
}

export interface Attachment {
  id: number;
  fileName: string;
  fileSize?: number;
  fileType?: string;
  uploadedAt: string;
}

export interface ComplaintFilter {
  search?: string;
  status?: string;
  priority?: string;
  channel?: string;
  complaintTypeId?: number;
  agencyId?: number;
  isEscalated?: boolean;
  fromDate?: string;
  toDate?: string;
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDir?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface DashboardStats {
  totalComplaints: number;
  newComplaints: number;
  inProgressComplaints: number;
  closedToday: number;
  escalatedComplaints: number;
  slaBreachedComplaints: number;
  averageResolutionHours: number;
  slaComplianceRate: number;
  byType: { type: string; count: number }[];
  byStatus: { status: string; count: number }[];
  byAgency: { agency: string; count: number }[];
  byPriority: { priority: string; count: number }[];
  dailyTrend: { date: string; created: number; closed: number }[];
  agentPerformance: AgentPerformance[];
}

export interface AgentPerformance {
  agentName: string;
  totalAssigned: number;
  totalClosed: number;
  avgResolutionHours: number;
  slaRate: number;
}

export interface Notification {
  id: number;
  title: string;
  message: string;
  type?: string;
  complaintId?: number;
  isRead: boolean;
  createdAt: string;
}

export const COMPLAINT_STATUSES = ['Nouvelle', 'Assignée', 'En cours', 'Validation', 'Clôturée', 'Rejetée'];
export const COMPLAINT_PRIORITIES = ['Faible', 'Moyenne', 'Haute', 'Critique'];
export const COMPLAINT_CHANNELS = ['Agence', 'Téléphone', 'E-Banking', 'Email', 'Autre'];

export interface AuditLog {
  id: number;
  userId?: number;
  userName?: string;
  userEmail?: string;
  action: string;
  entity?: string;
  entityId?: number;
  detail?: string;
  ipAddress?: string;
  createdAt: string;
}

export interface AuditFilter {
  search?: string;
  from?: string;
  to?: string;
  action?: string;
  userId?: number;
  page: number;
  pageSize: number;
}

export const AUDIT_ACTIONS = [
  { value: '', label: 'Toutes actions' },
  { value: 'LOGIN', label: 'LOGIN' },
  { value: 'LOGOUT', label: 'LOGOUT' },
  { value: 'REGISTER', label: 'REGISTER' },
  { value: 'CREATE', label: 'CREATE' },
  { value: 'STATUS', label: 'STATUS' },
  { value: 'ASSIGN', label: 'ASSIGN' },
  { value: 'COMMENT', label: 'COMMENT' },
  { value: 'ESCALADE', label: 'ESCALADE' },
  { value: 'ESCALATION', label: 'ESCALATION' }
];

export interface AuditPageResult {
  items: AuditLog[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface UserListItem {
  id: number;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  roleId: number;
  roleName: string;
  agencyId?: number;
  agencyName?: string;
  avatarUrl?: string; // Ajouté pour permettre l'affichage des images
  isActive: boolean;
  emailVerified: boolean;
  professionalId?: string;
  governorate?: string;
  gender?: string;
  lastLogin?: string;
  openComplaintsCount: number;
}



export interface UserStats {
  totalActive: number;
  rolesCount: number;
  byRole: { roleName: string; count: number }[];
}

export interface RoleOption {
  id: number;
  name: string;
}

export interface AgencyOption {
  id: number;
  name: string;
  governorate?: string;
}

export interface AiMessage {
  role: string;
  content: string;
}

export interface AiChatRequest {
  message: string;
  complaintId?: number;
  conversationHistory?: AiMessage[];
  /** Pièce jointe en base64 (image ou PDF) pour aider à comprendre la plateforme */
  attachmentBase64?: string;
  attachmentMimeType?: string;
  attachmentFileName?: string;
}

export interface AiSuggestion {
  type: string;
  priority: string;
  suggestedAgent: string;
  confidence: number;
}

export interface AiChatResponse {
  reply: string;
  suggestions?: AiSuggestion;
}

export interface AiConversationHistory {
  id: number;
  complaintId?: number;
  messages: AiMessage[];
  createdAt: string;
  updatedAt: string;
}

/** Vue réclamation pour le suivi client (référence + email). */
export interface ClientComplaintView {
  id: number;
  reference: string;
  title: string;
  description: string;
  complaintType: string;
  channel: string;
  priority: string;
  status: string;
  resolutionNote?: string;
  rejectionReason?: string;
  satisfactionRating?: number;
  speedRating?: number;
  qualityRating?: number;
  clientFeedback?: string;
  createdAt: string;
  closedAt?: string;
  statusHistory: StatusHistory[];
  comments: Comment[];
  attachments: Attachment[];
}
