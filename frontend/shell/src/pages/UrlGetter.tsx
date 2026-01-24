import React, { useState, useEffect } from 'react';
import { useMsal } from '@azure/msal-react';
import { Button } from '../components/ui/button';
import { Card } from '../components/ui/card';
import { Input } from '../components/ui/input';
import { Label } from '../components/ui/label';
import { Textarea } from '../components/ui/textarea';
import { loginRequest } from '../authConfig';

interface Header {
  name: string;
  value: string;
}

export const UrlGetter = () => {
  const { instance, accounts } = useMsal();
  const [method, setMethod] = useState('POST');
  const [endpoint, setEndpoint] = useState('https://localhost:7201/api/Orders');
  const [headers, setHeaders] = useState<Header[]>([
    { name: 'Content-Type', value: 'application/json' },
    { name: 'Authorization', value: 'Bearer Loading...' },
    { name: 'x-ms-session-id', value: crypto.randomUUID() }
  ]);
  const [requestBody, setRequestBody] = useState(JSON.stringify({
    name: "Sample Order",
    quantity: 10,
    totalCost: 250
  }, null, 2));
  const [responseBody, setResponseBody] = useState('');
  const [responseHeaders, setResponseHeaders] = useState('');
  const [loading, setLoading] = useState(false);
  const [activeTab, setActiveTab] = useState('headers');
  const [responseTab, setResponseTab] = useState('body');

  // Mask bearer token for display
  const maskBearerToken = (value: string): string => {
    if (value.startsWith('Bearer ')) {
      const token = value.substring(7); // Remove "Bearer "
      if (token && token !== 'Loading...' && token !== '[Token acquisition failed]') {
        return 'Bearer ' + '*'.repeat(32);
      }
    }
    return value;
  };

  // Get display value for header (mask token if it's Authorization)
  const getDisplayValue = (header: Header): string => {
    if (header.name === 'Authorization') {
      return maskBearerToken(header.value);
    }
    return header.value;
  };

  // Fetch and populate bearer token from MSAL
  useEffect(() => {
    const getToken = async () => {
      if (accounts.length > 0) {
        try {
          const request = {
            ...loginRequest,
            account: accounts[0],
          };
          const response = await instance.acquireTokenSilent(request);
          
          // Update the Authorization header with the actual token
          setHeaders(prevHeaders => 
            prevHeaders.map(h => 
              h.name === 'Authorization' 
                ? { ...h, value: `Bearer ${response.accessToken}` }
                : h
            )
          );
        } catch (error) {
          console.error('Failed to acquire token:', error);
          // Try to acquire token interactively
          try {
            const response = await instance.acquireTokenPopup(loginRequest);
            setHeaders(prevHeaders => 
              prevHeaders.map(h => 
                h.name === 'Authorization' 
                  ? { ...h, value: `Bearer ${response.accessToken}` }
                  : h
              )
            );
          } catch (popupError) {
            console.error('Failed to acquire token via popup:', popupError);
            setHeaders(prevHeaders => 
              prevHeaders.map(h => 
                h.name === 'Authorization' 
                  ? { ...h, value: 'Bearer [Token acquisition failed]' }
                  : h
              )
            );
          }
        }
      }
    };

    getToken();
  }, [instance, accounts]);

  const addHeader = () => {
    setHeaders([...headers, { name: '', value: '' }]);
  };

  const updateHeader = (index: number, field: 'name' | 'value', value: string) => {
    const newHeaders = [...headers];
    newHeaders[index][field] = value;
    setHeaders(newHeaders);
  };

  const removeHeader = (index: number) => {
    setHeaders(headers.filter((_, i) => i !== index));
  };

  const executeRequest = async () => {
    setLoading(true);
    setResponseBody('');
    setResponseHeaders('');

    try {
      const headerObj: Record<string, string> = {};
      headers.forEach(h => {
        if (h.name && h.value) {
          headerObj[h.name] = h.value;
        }
      });

      const options: RequestInit = {
        method,
        headers: headerObj,
      };

      if (method !== 'GET' && requestBody) {
        options.body = requestBody;
      }

      const response = await fetch(endpoint, options);
      const responseText = await response.text();
      
      // Format response body
      try {
        const jsonResponse = JSON.parse(responseText);
        setResponseBody(JSON.stringify(jsonResponse, null, 2));
      } catch {
        setResponseBody(responseText);
      }

      // Format response headers
      const headersObj: Record<string, string> = {};
      response.headers.forEach((value, key) => {
        headersObj[key] = value;
      });
      setResponseHeaders(JSON.stringify(headersObj, null, 2));
      setResponseTab('body');
    } catch (error) {
      setResponseBody(`Error: ${error instanceof Error ? error.message : String(error)}`);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-6 max-w-7xl mx-auto">
      <div className="mb-6">
        <h1 className="text-3xl font-bold mb-2">API Playground (preview)</h1>
        <button 
          className="text-sm text-blue-600 hover:underline"
          onClick={addHeader}
        >
          + New request
        </button>
      </div>

      <Card className="p-6">
        <div className="space-y-6">
          {/* HTTP Method and Endpoint */}
          <div className="flex gap-4 items-end">
            <div className="w-32">
              <Label htmlFor="method">Method</Label>
              <select
                id="method"
                value={method}
                onChange={(e) => setMethod(e.target.value)}
                className="w-full mt-1 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="GET">GET</option>
                <option value="POST">POST</option>
                <option value="PUT">PUT</option>
                <option value="DELETE">DELETE</option>
                <option value="PATCH">PATCH</option>
              </select>
            </div>
            <div className="flex-1">
              <Label htmlFor="endpoint">API endpoint</Label>
              <Input
                id="endpoint"
                value={endpoint}
                onChange={(e) => setEndpoint(e.target.value)}
                placeholder="Enter API endpoint URL"
                className="mt-1"
              />
            </div>
            <Button 
              onClick={executeRequest}
              disabled={loading}
              className="px-8"
            >
              {loading ? 'Executing...' : 'Execute'}
            </Button>
          </div>

          {/* Request Tabs */}
          <div>
            <div className="border-b border-gray-200">
              <nav className="-mb-px flex space-x-8">
                <button
                  onClick={() => setActiveTab('headers')}
                  className={`py-2 px-1 border-b-2 font-medium text-sm ${
                    activeTab === 'headers'
                      ? 'border-blue-500 text-blue-600'
                      : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                  }`}
                >
                  Request headers
                </button>
                <button
                  onClick={() => setActiveTab('body')}
                  className={`py-2 px-1 border-b-2 font-medium text-sm ${
                    activeTab === 'body'
                      ? 'border-blue-500 text-blue-600'
                      : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                  }`}
                >
                  Request body
                </button>
              </nav>
            </div>

            <div className="mt-4">
              {activeTab === 'headers' && (
                <div className="space-y-2">
                  <div className="grid grid-cols-2 gap-4 mb-2 font-medium text-sm">
                    <div>Name</div>
                    <div>Value</div>
                  </div>
                  {headers.map((header, index) => (
                    <div key={index} className="grid grid-cols-2 gap-4 items-center">
                      <Input
                        value={header.name}
                        onChange={(e) => updateHeader(index, 'name', e.target.value)}
                        placeholder="Header name"
                        readOnly={header.name === 'Authorization'}
                      />
                      <div className="flex gap-2">
                        <Input
                          value={getDisplayValue(header)}
                          onChange={(e) => updateHeader(index, 'value', e.target.value)}
                          placeholder="Header value"
                          className="flex-1"
                          readOnly={header.name === 'Authorization'}
                        />
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => removeHeader(index)}
                        >
                          ✕
                        </Button>
                      </div>
                    </div>
                  ))}
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={addHeader}
                    className="mt-2"
                  >
                    + Add header
                  </Button>
                </div>
              )}

              {activeTab === 'body' && (
                <div>
                  <Textarea
                    value={requestBody}
                    onChange={(e) => setRequestBody(e.target.value)}
                    placeholder="Enter request body (JSON)"
                    className="font-mono text-sm min-h-[200px]"
                  />
                </div>
              )}
            </div>
          </div>

          {/* Response Section */}
          {(responseBody || responseHeaders) && (
            <div className="border-t pt-6">
              <h3 className="text-lg font-semibold mb-4">Response</h3>
              <div className="border-b border-gray-200">
                <nav className="-mb-px flex space-x-8">
                  <button
                    onClick={() => setResponseTab('body')}
                    className={`py-2 px-1 border-b-2 font-medium text-sm ${
                      responseTab === 'body'
                        ? 'border-blue-500 text-blue-600'
                        : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                    }`}
                  >
                    Response body
                  </button>
                  <button
                    onClick={() => setResponseTab('headers')}
                    className={`py-2 px-1 border-b-2 font-medium text-sm ${
                      responseTab === 'headers'
                        ? 'border-blue-500 text-blue-600'
                        : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                    }`}
                  >
                    Response headers
                  </button>
                </nav>
              </div>

              <div className="mt-4">
                <pre className="bg-gray-50 p-4 rounded-md overflow-auto font-mono text-sm min-h-[200px]">
                  {responseTab === 'body' ? responseBody : responseHeaders}
                </pre>
              </div>
            </div>
          )}
        </div>
      </Card>
    </div>
  );
};
