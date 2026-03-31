export interface AuditLogDto {
  id: string;
  username?: string;
  userRole?: string;
  action: string;
  entityName: string;
  entityId: string;
  oldValues?: string;
  newValues?: string;
  userAgent?: string;
  timestamp: string;
  status: string;
  description?: string;
}

export interface AuditLogFilterDto {
  userId?: string;
  action?: string;
  entityName?: string;
  from?: string;
  to?: string;
  status?: string;
  page: number;
  pageSize: number;
}
