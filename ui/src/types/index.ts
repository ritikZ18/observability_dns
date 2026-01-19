export enum CheckType {
  DNS = 0,
  TLS = 1,
  HTTP = 2
}

export enum IncidentStatus {
  OPEN = 0,
  RESOLVED = 1,
  ACKNOWLEDGED = 2
}

export enum Severity {
  LOW = 0,
  MEDIUM = 1,
  HIGH = 2,
  CRITICAL = 3
}

export interface Domain {
  id: string;
  name: string;
  enabled: boolean;
  intervalMinutes: number;
  createdAt: string;
  updatedAt: string;
}

export interface DomainDetail extends Domain {
  checks: Check[];
  recentRuns: ProbeRun[];
  openIncidents: Incident[];
}

export interface Check {
  id: string;
  checkType: CheckType;
  enabled: boolean;
}

export interface ProbeRun {
  id: string;
  domainId: string;
  domainName: string;
  checkType: CheckType;
  success: boolean;
  errorCode?: string;
  errorMessage?: string;
  dnsMs?: number;
  tlsMs?: number;
  ttfbMs?: number;
  totalMs: number;
  statusCode?: number;
  recordsSnapshot?: any;
  certificateInfo?: any;
  startedAt: string;
  completedAt: string;
}

export interface Incident {
  id: string;
  domainId: string;
  domainName: string;
  checkType: CheckType;
  severity: Severity;
  status: IncidentStatus;
  reason?: string;
  startedAt: string;
  resolvedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface WebsiteInfo {
  domain: string;
  ipAddresses: string[];
  dnsRecords: DnsRecord[];
  tlsInfo?: TlsInfo;
  fetchedAt: string;
}

export interface DnsRecord {
  type: string;
  value: string;
  ttl?: number;
}

export interface TlsInfo {
  isValid: boolean;
  issuer?: string;
  subject?: string;
  notBefore?: string;
  notAfter?: string;
  daysUntilExpiry?: number;
  subjectAlternativeNames: string[];
  errorMessage?: string;
}

export interface CreateDomainRequest {
  name: string;
  intervalMinutes: number;
  enabled: boolean;
}

export interface UpdateDomainRequest {
  enabled?: boolean;
  intervalMinutes?: number;
}
