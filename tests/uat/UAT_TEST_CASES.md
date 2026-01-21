# User Acceptance Test Cases

## Overview

This document contains manual test cases for User Acceptance Testing (UAT) of the DNS & TLS Observatory.

## Test Environment

- **Environment**: [Local/Staging/Production]
- **Date**: [Date]
- **Tester**: [Name]
- **Version**: [Version]

---

## Test Suite 1: Domain Management

### TC-001: Add Domain
**Priority**: High  
**Preconditions**: User is on the dashboard

**Steps**:
1. Navigate to dashboard
2. Enter domain name: `example.com`
3. Select check interval: `5 minutes`
4. Select icon (optional)
5. Select group (optional)
6. Click "ADD DOMAIN"

**Expected Result**:
- Domain appears in observability table
- Success message displayed
- Domain shows "Unknown" status initially

**Actual Result**: [ ]  
**Status**: [ ] Pass / [ ] Fail / [ ] Blocked

---

### TC-002: View Domain Details
**Priority**: High  
**Preconditions**: At least one domain exists

**Steps**:
1. Click on a domain in the observability table
2. View domain detail page

**Expected Result**:
- Domain details page loads
- Shows DNS, TLS, HTTP check results
- Displays charts/graphs
- Shows recent probe runs

**Actual Result**: [ ]  
**Status**: [ ] Pass / [ ] Fail / [ ] Blocked

---

### TC-003: Delete Domain
**Priority**: Medium  
**Preconditions**: At least one domain exists

**Steps**:
1. Click "DELETE" button for a domain
2. Confirm deletion

**Expected Result**:
- Domain removed from table
- Success message displayed
- Domain no longer appears in list

**Actual Result**: [ ]  
**Status**: [ ] Pass / [ ] Fail / [ ] Blocked

---

## Test Suite 2: Group Management

### TC-004: Create Group
**Priority**: High  
**Preconditions**: User is on the dashboard

**Steps**:
1. Click "CREATE GROUP"
2. Enter group name: `Test Group`
3. Enter description (optional)
4. Select color
5. Select icon
6. Click "CREATE"

**Expected Result**:
- Group created successfully
- Group appears in groups section
- Group can be selected when adding domains

**Actual Result**: [ ]  
**Status**: [ ] Pass / [ ] Fail / [ ] Blocked

---

### TC-005: View Group Dashboard
**Priority**: Medium  
**Preconditions**: At least one group exists with domains

**Steps**:
1. Click on a group card
2. View group detail page

**Expected Result**:
- Group dashboard loads
- Shows group statistics
- Lists all domains in group
- Displays aggregate metrics

**Actual Result**: [ ]  
**Status**: [ ] Pass / [ ] Fail / [ ] Blocked

---

## Test Suite 3: Observability Features

### TC-006: View Probe Results
**Priority**: High  
**Preconditions**: Domain exists and has probe runs

**Steps**:
1. Wait for probe to run (based on interval)
2. View domain in observability table
3. Check status indicators

**Expected Result**:
- Status shows "Healthy" or "Unhealthy"
- Last check time updates
- DNS, TLS, HTTP status displayed
- Latency values shown

**Actual Result**: [ ]  
**Status**: [ ] Pass / [ ] Fail / [ ] Blocked

---

### TC-007: Filter by Group
**Priority**: Medium  
**Preconditions**: Multiple groups exist

**Steps**:
1. Select a group from filter dropdown
2. View filtered results

**Expected Result**:
- Only domains in selected group shown
- Filter persists when navigating
- Can clear filter

**Actual Result**: [ ]  
**Status**: [ ] Pass / [ ] Fail / [ ] Blocked

---

## Test Suite 4: Backup & Restore

### TC-008: Export Backup
**Priority**: Medium  
**Preconditions**: At least one domain/group exists

**Steps**:
1. Click "EXPORT BACKUP"
2. Download JSON file
3. Verify file contents

**Expected Result**:
- JSON file downloads
- Contains all domains and groups
- File is valid JSON

**Actual Result**: [ ]  
**Status**: [ ] Pass / [ ] Fail / [ ] Blocked

---

### TC-009: Import Backup
**Priority**: Medium  
**Preconditions**: Backup file exists

**Steps**:
1. Click "IMPORT BACKUP"
2. Select backup JSON file
3. Choose "Clear existing data" (optional)
4. Click "IMPORT"

**Expected Result**:
- Data imported successfully
- Domains and groups restored
- Success message displayed

**Actual Result**: [ ]  
**Status**: [ ] Pass / [ ] Fail / [ ] Blocked

---

## Test Suite 5: Error Handling

### TC-010: Invalid Domain Input
**Priority**: Medium

**Steps**:
1. Enter invalid domain: `not-a-domain`
2. Click "ADD DOMAIN"

**Expected Result**:
- Error message displayed
- Domain not added
- Form validation works

**Actual Result**: [ ]  
**Status**: [ ] Pass / [ ] Fail / [ ] Blocked

---

### TC-011: Network Error Handling
**Priority**: Low

**Steps**:
1. Disconnect network
2. Try to add domain

**Expected Result**:
- Error message displayed
- User-friendly error message
- App doesn't crash

**Actual Result**: [ ]  
**Status**: [ ] Pass / [ ] Fail / [ ] Blocked

---

## Test Summary

| Test Case | Priority | Status | Notes |
|-----------|----------|--------|-------|
| TC-001 | High | [ ] | |
| TC-002 | High | [ ] | |
| TC-003 | Medium | [ ] | |
| TC-004 | High | [ ] | |
| TC-005 | Medium | [ ] | |
| TC-006 | High | [ ] | |
| TC-007 | Medium | [ ] | |
| TC-008 | Medium | [ ] | |
| TC-009 | Medium | [ ] | |
| TC-010 | Medium | [ ] | |
| TC-011 | Low | [ ] | |

**Total Passed**: ___ / 11  
**Total Failed**: ___ / 11  
**Total Blocked**: ___ / 11

**Tester Signature**: _________________  
**Date**: _________________
