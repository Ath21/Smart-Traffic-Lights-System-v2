// src/utils/jwtHelper.js
import { jwtDecode } from "jwt-decode"

/**
 * Decode a JWT from localStorage or a string.
 * Returns { sub, email, role, status, exp, iss, aud }
 */
export function decodeToken(token) {
  try {
    if (!token) return null
    const decoded = jwtDecode(token)

    // normalize role key
    const role =
      decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ||
      decoded.role ||
      null

    return {
      id: decoded.sub,
      email:
        decoded.email ||
        decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"],
      role,
      status: decoded.status,
      exp: decoded.exp,
      iss: decoded.iss,
      aud: decoded.aud,
    }
  } catch (err) {
    console.warn("JWT decode failed:", err.message)
    return null
  }
}
