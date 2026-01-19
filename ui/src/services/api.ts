import axios from 'axios';
import type {
  Domain,
  DomainDetail,
  ProbeRun,
  Incident,
  WebsiteInfo,
  CreateDomainRequest,
  UpdateDomainRequest,
  CheckType
} from '../types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Domains API
export const domainsApi = {
  getAll: async (): Promise<Domain[]> => {
    const response = await apiClient.get<Domain[]>('/api/domains');
    return response.data;
  },

  getById: async (id: string): Promise<DomainDetail> => {
    const response = await apiClient.get<DomainDetail>(`/api/domains/${id}`);
    return response.data;
  },

  create: async (request: CreateDomainRequest): Promise<Domain> => {
    const response = await apiClient.post<Domain>('/api/domains', request);
    return response.data;
  },

  update: async (id: string, request: UpdateDomainRequest): Promise<void> => {
    await apiClient.put(`/api/domains/${id}`, request);
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/domains/${id}`);
  },
};

// Probe Runs API
export const probeRunsApi = {
  getByDomain: async (domainId: string, checkType?: CheckType, limit = 50): Promise<ProbeRun[]> => {
    const params = new URLSearchParams();
    if (checkType !== undefined) params.append('checkType', checkType.toString());
    params.append('limit', limit.toString());
    const response = await apiClient.get<ProbeRun[]>(`/api/probe-runs/domains/${domainId}?${params}`);
    return response.data;
  },

  getAll: async (limit = 100): Promise<ProbeRun[]> => {
    const response = await apiClient.get<ProbeRun[]>(`/api/probe-runs?limit=${limit}`);
    return response.data;
  },
};

// Incidents API
export const incidentsApi = {
  getAll: async (status?: IncidentStatus, domainId?: string): Promise<Incident[]> => {
    const params = new URLSearchParams();
    if (status !== undefined) params.append('status', status.toString());
    if (domainId) params.append('domainId', domainId);
    const response = await apiClient.get<Incident[]>(`/api/incidents?${params}`);
    return response.data;
  },

  getById: async (id: string): Promise<Incident> => {
    const response = await apiClient.get<Incident>(`/api/incidents/${id}`);
    return response.data;
  },

  resolve: async (id: string): Promise<void> => {
    await apiClient.put(`/api/incidents/${id}/resolve`);
  },
};

// Website Info API
export const websiteInfoApi = {
  get: async (domain: string): Promise<WebsiteInfo> => {
    const response = await apiClient.get<WebsiteInfo>(`/api/website-info/${encodeURIComponent(domain)}`);
    return response.data;
  },
};
