import { NextRequest, NextResponse } from 'next/server';
import { getSession, getAccessToken } from '@/lib/auth';

/**
 * GET /api/auth/me
 * Retorna dados da sessão atual e seta um cookie público (não HttpOnly)
 * com o token para que o Blazor WASM possa ler via document.cookie.
 *
 * Nota: o cookie público contém apenas o access token (curta duração).
 * O refresh token permanece HttpOnly e seguro.
 */
export async function GET(request: NextRequest) {
  const session = await getSession();

  if (!session) {
    return NextResponse.json({ authenticated: false }, { status: 401 });
  }

  const token = await getAccessToken();
  const isProduction = process.env.NODE_ENV === 'production';

  const response = NextResponse.json({
    authenticated: true,
    username: session.username,
    email: session.email,
    roles: session.roles,
  });

  // Cookie público (acessível por JS) para o Blazor WASM
  if (token) {
    const cookieOpts = [
      'Path=/',
      'SameSite=Strict',
      `Max-Age=${60 * 60}`, // 1h
      isProduction ? 'Secure' : '',
    ]
      .filter(Boolean)
      .join('; ');

    response.headers.append(
      'Set-Cookie',
      `ims_public_token=${token}; ${cookieOpts}`
    );
  }

  return response;
}
