export const ROLES = ['Administrador', 'Gerente', 'Funcionario'] as const;

export interface User {
  readonly id: number;
  readonly name: string;
  readonly email: string;
  readonly active: boolean;
  readonly roles: readonly string[];
}

export interface Session {
  readonly name: string;
  readonly email: string;
  readonly roles: readonly string[];
}

export interface CreateUserRequest {
  readonly name: string;
  readonly email: string;
  readonly password: string;
  readonly roles: readonly string[];
}
