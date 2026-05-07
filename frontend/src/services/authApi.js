const API_BASE_URL =
  import.meta.env.VITE_ROLES_API_BASE_URL || "http://localhost:5215";

const parseResponse = async (response) => {
  const contentType = response.headers.get("content-type") || "";
  const body = contentType.includes("application/json")
    ? await response.json()
    : null;
  if (!response.ok) {
    throw new Error(
      body?.message || `Request failed with status ${response.status}`,
    );
  }
  return body;
};

export const login = async ({ email, password, rememberMe = false }) => {
  const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password, rememberMe }),
  });
  return parseResponse(response);
};

export const verifyOtp = async ({ challengeId, code }) => {
  const response = await fetch(`${API_BASE_URL}/api/auth/verify-otp`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ challengeId, code }),
  });
  return parseResponse(response);
};
