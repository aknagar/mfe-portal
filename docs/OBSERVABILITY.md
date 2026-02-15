# Observability

## Azure Monitor vs Log Analytics vs Azure Application Insights

These three Azure services work together to provide comprehensive observability for your applications. Understanding their relationship helps you use them effectively.

### Azure Monitor
- **What it is**: The umbrella observability platform for all Azure resources
- **Scope**: Platform-level monitoring across all Azure services
- **Key capabilities**:
  - Collects metrics and logs from all Azure resources
  - Provides alerting, dashboards, and visualizations
  - Includes specialized monitoring solutions (Application Insights, Container Insights, VM Insights)
  - Centralizes data from multiple sources
- **Think of it as**: The entire monitoring ecosystem

### Log Analytics Workspace
- **What it is**: The log storage and query engine within Azure Monitor
- **Scope**: Stores and analyzes log data
- **Key capabilities**:
  - Data repository for Azure Monitor logs
  - Query engine using Kusto Query Language (KQL)
  - Retention and archival of log data
  - Workspace-based access control
- **Think of it as**: The database where your logs live

### Azure Application Insights
- **What it is**: Application Performance Management (APM) service within Azure Monitor
- **Scope**: Application-level monitoring and diagnostics
- **Key capabilities**:
  - Tracks application requests, dependencies, and exceptions
  - Distributed tracing across microservices
  - Performance monitoring (response times, failure rates)
  - User analytics and custom telemetry
  - Automatic instrumentation for .NET, Node.js, Java, Python
- **Think of it as**: Your application's health monitor

### How They Work Together

```
┌─────────────────────────────────────────────────────────┐
│                     Azure Monitor                        │
│  (Overall monitoring platform)                           │
│                                                          │
│  ┌──────────────────────────┐  ┌────────────────────┐  │
│  │ Application Insights     │  │  Other Services    │  │
│  │ (Application telemetry)  │  │  (VM Insights,     │  │
│  │                          │  │   Container        │  │
│  │  - Requests              │  │   Insights, etc.)  │  │
│  │  - Dependencies          │  │                    │  │
│  │  - Exceptions            │  │                    │  │
│  │  - Custom events         │  │                    │  │
│  └──────────┬───────────────┘  └─────────┬──────────┘  │
│             │                            │              │
│             └────────────┬───────────────┘              │
│                          ▼                              │
│             ┌─────────────────────────┐                 │
│             │ Log Analytics Workspace │                 │
│             │ (Log storage + KQL)     │                 │
│             └─────────────────────────┘                 │
└─────────────────────────────────────────────────────────┘
```

### In Practice

When you configure Application Insights:
1. **Application Insights** collects telemetry from your application
2. Data is sent to **Azure Monitor**
3. Logs are stored in a **Log Analytics Workspace**
4. You query the data using KQL in the workspace
5. You view insights, alerts, and dashboards through **Azure Monitor**

### Example Workflow

```csharp
// Your application sends telemetry
_telemetry.TrackEvent("OrderProcessed");

// ↓ Goes to Application Insights
// ↓ Flows into Azure Monitor
// ↓ Stored in Log Analytics Workspace

// You query it with KQL
customEvents
| where name == "OrderProcessed"
| summarize count() by bin(timestamp, 1h)
```

### Summary

| Service | Role | Analogy |
|---------|------|---------|
| **Azure Monitor** | Overall platform | The monitoring control center |
| **Log Analytics Workspace** | Storage + querying | The log database |
| **Application Insights** | Application monitoring | The application health sensor |

**Key takeaway**: Application Insights is a component of Azure Monitor that specializes in application telemetry, and it stores its data in Log Analytics workspaces where you can query it with KQL.
