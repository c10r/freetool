import { Toaster } from "@/components/ui/toaster";
import { Toaster as Sonner } from "@/components/ui/sonner";
import { TooltipProvider } from "@/components/ui/tooltip";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import Index from "./pages/Index";
import RunApp from "./pages/RunApp";
import NotFound from "./pages/NotFound";

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
          <Route path="/workspaces/:nodeId" element={<Index />} />
          <Route path="/workspaces/:nodeId/run" element={<RunApp />} />
          <Route path="/resources" element={<Index />} />
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
