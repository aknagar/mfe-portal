import * as React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useMsal } from '@azure/msal-react';
import { 
  LayoutDashboard, 
  Settings, 
  Users, 
  Wrench,
  Menu,
  ChevronLeft,
  Smile,
  Code,
  User
} from 'lucide-react';
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarInset,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarTrigger,
} from './ui/sidebar';
import { AuthButton } from './AuthButton';

interface AdminLayoutProps {
  children?: React.ReactNode;
}

const navigation = [
  { name: 'Dashboard', href: '/', icon: LayoutDashboard },
  { name: 'Users', href: '/users', icon: Users },
  { name: 'Settings', href: '/settings', icon: Settings },
  { name: 'Hello World', href: '/hello-world', icon: Smile },
  { name: 'API Playground', href: '/api-playground', icon: Code },
];

export const AdminLayout: React.FC<AdminLayoutProps> = ({ children }) => {
  const location = useLocation();
  const { accounts } = useMsal();
  const isAuthenticated = accounts.length > 0;
  const account = isAuthenticated ? accounts[0] : null;

  return (
    <SidebarProvider defaultOpen={true}>
      <Sidebar collapsible="icon">
        <SidebarHeader>
          <div className="flex items-center gap-2 px-2 py-4">
            <Wrench className="h-6 w-6" />
            <h1 className="text-xl font-bold group-data-[collapsible=icon]:hidden">My Tools</h1>
          </div>
        </SidebarHeader>
        <SidebarContent>
          <SidebarGroup>
            <SidebarGroupLabel className="group-data-[collapsible=icon]:hidden">Navigation</SidebarGroupLabel>
            <SidebarGroupContent>
              <SidebarMenu>
                {navigation.map((item) => (
                  <SidebarMenuItem key={item.name}>
                    <SidebarMenuButton asChild isActive={location.pathname === item.href}>
                      <Link to={item.href}>
                        <item.icon className="h-4 w-4" />
                        <span className="group-data-[collapsible=icon]:hidden">{item.name}</span>
                      </Link>
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                ))}
              </SidebarMenu>
            </SidebarGroupContent>
          </SidebarGroup>
        </SidebarContent>
        <SidebarFooter>
          {isAuthenticated && account ? (
            <div className="flex items-center gap-3 px-2 py-3 group-data-[collapsible=icon]:justify-center">
              <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-primary text-primary-foreground">
                <User className="h-4 w-4" />
              </div>
              <div className="flex flex-col overflow-hidden group-data-[collapsible=icon]:hidden">
                <span className="text-sm font-medium truncate">
                  {account.name || 'User'}
                </span>
                <span className="text-xs text-muted-foreground truncate">
                  {account.username}
                </span>
              </div>
            </div>
          ) : (
            <div className="px-2 py-2 text-sm text-muted-foreground group-data-[collapsible=icon]:hidden">
              Please sign in
            </div>
          )}
        </SidebarFooter>
      </Sidebar>
      <SidebarInset>
        <header className="sticky top-0 z-40 flex h-16 items-center gap-4 border-b bg-background px-4 lg:px-6">
          <SidebarTrigger className="p-2 rounded-md hover:bg-accent">
            <Menu className="h-5 w-5" />
          </SidebarTrigger>
          <div className="flex-1" />
          <AuthButton />
        </header>
        <main className="p-6">
          {children}
        </main>
      </SidebarInset>
    </SidebarProvider>
  );
};
