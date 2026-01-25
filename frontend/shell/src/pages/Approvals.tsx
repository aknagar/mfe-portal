import * as React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Button } from '../components/ui/button';
import { config } from '../config';
import { CheckCircle, XCircle, Clock, RefreshCw, AlertCircle } from 'lucide-react';

interface ApprovalRequest {
  orderId: string;
  orderName: string;
  totalCost: number;
  quantity: number;
  status: 'Pending' | 'Approved' | 'Rejected' | 'TimedOut';
  createdAt: string;
  expiresAt: string;
  processedAt?: string;
  processedBy?: string;
  comments?: string;
}

const formatCurrency = (amount: number): string => {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);
};

const formatDate = (dateString: string): string => {
  return new Date(dateString).toLocaleString();
};

const getTimeRemaining = (expiresAt: string): string => {
  const now = new Date();
  const expires = new Date(expiresAt);
  const diff = expires.getTime() - now.getTime();

  if (diff <= 0) return 'Expired';

  const hours = Math.floor(diff / (1000 * 60 * 60));
  const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));

  if (hours > 0) {
    return `${hours}h ${minutes}m remaining`;
  }
  return `${minutes}m remaining`;
};

const getStatusBadge = (status: ApprovalRequest['status']) => {
  switch (status) {
    case 'Pending':
      return (
        <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800">
          <Clock className="h-3 w-3" />
          Pending
        </span>
      );
    case 'Approved':
      return (
        <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800">
          <CheckCircle className="h-3 w-3" />
          Approved
        </span>
      );
    case 'Rejected':
      return (
        <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-red-100 text-red-800">
          <XCircle className="h-3 w-3" />
          Rejected
        </span>
      );
    case 'TimedOut':
      return (
        <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-800">
          <AlertCircle className="h-3 w-3" />
          Timed Out
        </span>
      );
  }
};

export const Approvals: React.FC = () => {
  const [approvals, setApprovals] = React.useState<ApprovalRequest[]>([]);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);
  const [processingId, setProcessingId] = React.useState<string | null>(null);

  const fetchApprovals = React.useCallback(async () => {
    try {
      setError(null);
      const response = await fetch(`${config.api.baseUrl}/api/Approvals`);
      if (!response.ok) {
        throw new Error(`Failed to fetch approvals: ${response.statusText}`);
      }
      const data = await response.json();
      setApprovals(data);
    } catch (err) {
      console.error('Failed to fetch approvals:', err);
      setError(err instanceof Error ? err.message : 'Failed to fetch approvals');
    } finally {
      setLoading(false);
    }
  }, []);

  React.useEffect(() => {
    fetchApprovals();
    // Auto-refresh every 30 seconds
    const interval = setInterval(fetchApprovals, 30000);
    return () => clearInterval(interval);
  }, [fetchApprovals]);

  const handleApprove = async (orderId: string) => {
    if (!confirm('Are you sure you want to approve this order?')) return;

    setProcessingId(orderId);
    try {
      const response = await fetch(`${config.api.baseUrl}/api/Approvals/${orderId}/approve`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ approvedBy: 'Manager', comments: 'Approved via UI' }),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.message || 'Failed to approve order');
      }

      await fetchApprovals();
    } catch (err) {
      console.error('Failed to approve:', err);
      alert(err instanceof Error ? err.message : 'Failed to approve order');
    } finally {
      setProcessingId(null);
    }
  };

  const handleReject = async (orderId: string) => {
    const reason = prompt('Please provide a reason for rejection:');
    if (reason === null) return; // User cancelled

    setProcessingId(orderId);
    try {
      const response = await fetch(`${config.api.baseUrl}/api/Approvals/${orderId}/reject`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ approvedBy: 'Manager', comments: reason || 'Rejected via UI' }),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.message || 'Failed to reject order');
      }

      await fetchApprovals();
    } catch (err) {
      console.error('Failed to reject:', err);
      alert(err instanceof Error ? err.message : 'Failed to reject order');
    } finally {
      setProcessingId(null);
    }
  };

  const pendingApprovals = approvals.filter(a => a.status === 'Pending');
  const processedApprovals = approvals.filter(a => a.status !== 'Pending');

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Approvals</h1>
          <p className="text-muted-foreground mt-2">
            Review and manage order approval requests
          </p>
        </div>
        <Button variant="outline" onClick={fetchApprovals} disabled={loading}>
          <RefreshCw className={`h-4 w-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
          Refresh
        </Button>
      </div>

      {error && (
        <Card className="border-red-200 bg-red-50">
          <CardContent className="pt-6">
            <p className="text-red-800">{error}</p>
          </CardContent>
        </Card>
      )}

      {/* Pending Approvals */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Clock className="h-5 w-5 text-yellow-600" />
            Pending Approvals
          </CardTitle>
          <CardDescription>
            Orders requiring your review. Approvals expire after 24 hours.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="text-center py-8">
              <RefreshCw className="h-8 w-8 animate-spin mx-auto text-muted-foreground" />
              <p className="mt-2 text-muted-foreground">Loading approvals...</p>
            </div>
          ) : pendingApprovals.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              <CheckCircle className="h-12 w-12 mx-auto mb-4 text-green-500" />
              <p>No pending approvals</p>
            </div>
          ) : (
            <div className="space-y-4">
              {pendingApprovals.map((approval) => (
                <div
                  key={approval.orderId}
                  className="flex flex-col md:flex-row md:items-center justify-between p-4 border rounded-lg hover:bg-muted/50 transition-colors"
                >
                  <div className="space-y-1 mb-4 md:mb-0">
                    <div className="flex items-center gap-2">
                      <span className="font-semibold">{approval.orderName}</span>
                      {getStatusBadge(approval.status)}
                    </div>
                    <p className="text-sm text-muted-foreground">
                      Order ID: {approval.orderId}
                    </p>
                    <p className="text-sm">
                      <span className="font-medium">{approval.quantity}x</span> at{' '}
                      <span className="font-medium text-green-600">
                        {formatCurrency(approval.totalCost)}
                      </span>
                    </p>
                    <p className="text-xs text-muted-foreground">
                      Requested: {formatDate(approval.createdAt)}
                    </p>
                    <p className="text-xs text-orange-600 font-medium">
                      {getTimeRemaining(approval.expiresAt)}
                    </p>
                  </div>
                  <div className="flex gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleReject(approval.orderId)}
                      disabled={processingId === approval.orderId}
                    >
                      <XCircle className="h-4 w-4 mr-1" />
                      Reject
                    </Button>
                    <Button
                      size="sm"
                      onClick={() => handleApprove(approval.orderId)}
                      disabled={processingId === approval.orderId}
                    >
                      <CheckCircle className="h-4 w-4 mr-1" />
                      Approve
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Processed Approvals History */}
      {processedApprovals.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Approval History</CardTitle>
            <CardDescription>Previously processed approval requests</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {processedApprovals.map((approval) => (
                <div
                  key={approval.orderId}
                  className="flex items-center justify-between p-3 border rounded-lg bg-muted/30"
                >
                  <div className="space-y-1">
                    <div className="flex items-center gap-2">
                      <span className="font-medium">{approval.orderName}</span>
                      {getStatusBadge(approval.status)}
                    </div>
                    <p className="text-sm text-muted-foreground">
                      {approval.quantity}x at {formatCurrency(approval.totalCost)}
                    </p>
                    {approval.processedBy && (
                      <p className="text-xs text-muted-foreground">
                        Processed by {approval.processedBy} on {formatDate(approval.processedAt!)}
                      </p>
                    )}
                    {approval.comments && (
                      <p className="text-xs italic text-muted-foreground">
                        "{approval.comments}"
                      </p>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
};
