import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Route, Routes } from "react-router-dom";
import { Toaster as Sonner } from "@/components/ui/sonner";
import { Toaster } from "@/components/ui/toaster";
import { TooltipProvider } from "@/components/ui/tooltip";
import Index from "./pages/Index";
import NotFound from "./pages/NotFound";
import RunApp from "./pages/RunApp";

const queryClient = new QueryClient();

const App = () => (
  <QueryClientProvider client={queryClient}>
    <TooltipProvider>
      <Toaster />
      <Sonner />
      <BrowserRouter basename="/freetool">
        <Routes>
          <Route path="/" element={<Index />} />
          <Route path="/workspaces" element={<Index />} />
          <Route path="/workspaces/:workspaceId" element={<Index />} />
          <Route path="/workspaces/:workspaceId/:nodeId" element={<Index />} />
          <Route
            path="/workspaces/:workspaceId/:nodeId/run"
            element={<RunApp />}
          />
          <Route
            path="/workspaces/:workspaceId/resources"
            element={<Index />}
          />
          <Route path="/users" element={<Index />} />
          <Route path="/audit" element={<Index />} />
          {/* ADD ALL CUSTOM ROUTES ABOVE THE CATCH-ALL "*" ROUTE */}
          <Route path="*" element={<NotFound />} />
        </Routes>
      </BrowserRouter>
    </TooltipProvider>
  </QueryClientProvider>
);

export default App;
