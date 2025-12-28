const NotFound = () => {
  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <div className="text-center">
        <h1 className="text-4xl font-bold mb-4">404</h1>
        <p className="text-xl text-muted-foreground mb-4">
          Oops! Page not found
        </p>
        <a href="/freetool" className="text-primary underline">
          Return to Home
        </a>
      </div>
    </div>
  );
};

export default NotFound;
