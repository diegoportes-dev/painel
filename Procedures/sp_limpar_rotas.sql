CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_rotas`()
BEGIN
-- Garante que o fuso horário esteja correto para o preenchimento da Auditoria

call crud_central.sp_limpar_rotas();

SET @now = UTC_TIMESTAMP();
SET @createdBy = 'Sistema';

-- Bloco de Carga da Tabela de Rotas baseada no OpenAPI
INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
VALUES
-- ----------------------------------------------------
-- Endpoints de Autenticação / Configuração Inicial
-- ----------------------------------------------------
(UUID(), 'SetupCadastroSqlite', '/auth/setup-cadastro-sqlite', 'POST', 'HTTP: POST /auth/setup-cadastro-sqlite', 'Configurações', @now, @createdBy),
(UUID(), 'SetupCadastro', '/auth/setup-cadastro', 'POST', 'HTTP: POST /auth/setup-cadastro', 'Configurações', @now, @createdBy),
(UUID(), 'Login', '/login', 'POST', 'HTTP: POST /login', 'Autenticação', @now, @createdBy),
(UUID(), 'EsqueciSenha', '/login/esqueci-senha', 'POST', 'HTTP: POST /login/esqueci-senha', 'Autenticação', @now, @createdBy),
(UUID(), 'ResetarSenha', '/login/resetar-senha', 'POST', 'HTTP: POST /login/resetar-senha', 'Autenticação', @now, @createdBy),

-- ----------------------------------------------------
-- Endpoints do Cadastro de Perfis
-- ----------------------------------------------------
(UUID(), 'GetPerfis', '/perfis', 'GET', 'HTTP: GET /perfis', 'Configurações', @now, @createdBy),
(UUID(), 'PostPerfis', '/perfis', 'POST', 'HTTP: POST /perfis', 'Configurações', @now, @createdBy),
(UUID(), 'GetPerfisById', '/perfis/{id}', 'GET', 'HTTP: GET /perfis/{id}', 'Configurações', @now, @createdBy),
(UUID(), 'PutPerfisById', '/perfis/{id}', 'PUT', 'HTTP: PUT /perfis/{id}', 'Configurações', @now, @createdBy),
(UUID(), 'DeletePerfisById', '/perfis/{id}', 'DELETE', 'HTTP: DELETE /perfis/{id}', 'Configurações', @now, @createdBy),
(UUID(), 'PatchPerfisById', '/perfis/{id}', 'PATCH', 'HTTP: PATCH /perfis/{id}', 'Configurações', @now, @createdBy),

-- ----------------------------------------------------
-- Endpoints do Cadastro de Usuários
-- ----------------------------------------------------
(UUID(), 'GetUsuarios', '/usuarios', 'GET', 'HTTP: GET /usuarios', 'Cadastros', @now, @createdBy),
(UUID(), 'PostUsuarios', '/usuarios', 'POST', 'HTTP: POST /usuarios', 'Cadastros', @now, @createdBy),
(UUID(), 'GetUsuariosById', '/usuarios/{id}', 'GET', 'HTTP: GET /usuarios/{id}', 'Cadastros', @now, @createdBy),
(UUID(), 'PutUsuariosById', '/usuarios/{id}', 'PUT', 'HTTP: PUT /usuarios/{id}', 'Cadastros', @now, @createdBy),
(UUID(), 'DeleteUsuariosById', '/usuarios/{id}', 'DELETE', 'HTTP: DELETE /usuarios/{id}', 'Cadastros', @now, @createdBy),
(UUID(), 'PatchUsuariosById', '/usuarios/{id}', 'PATCH', 'HTTP: PATCH /usuarios/{id}', 'Cadastros', @now, @createdBy),

-- ----------------------------------------------------
-- Endpoints do Cadastro de Tenants (Empresas/Isolamentos)
-- ----------------------------------------------------
(UUID(), 'GetTenants', '/tenants', 'GET', 'HTTP: GET /tenants', 'Administração', @now, @createdBy),
(UUID(), 'PostTenants', '/tenants', 'POST', 'HTTP: POST /tenants', 'Administração', @now, @createdBy),
(UUID(), 'GetTenantsById', '/tenants/{id}', 'GET', 'HTTP: GET /tenants/{id}', 'Administração', @now, @createdBy),
(UUID(), 'PutTenantsById', '/tenants/{id}', 'PUT', 'HTTP: PUT /tenants/{id}', 'Administração', @now, @createdBy),
(UUID(), 'DeleteTenantsById', '/tenants/{id}', 'DELETE', 'HTTP: DELETE /tenants/{id}', 'Administração', @now, @createdBy),
(UUID(), 'PatchTenantsById', '/tenants/{id}', 'PATCH', 'HTTP: PATCH /tenants/{id}', 'Administração', @now, @createdBy);

END