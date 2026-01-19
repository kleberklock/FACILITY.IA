SELECT TOP (1000) [Id]
      ,[Name]
      ,[Email]
      ,[Password]
      ,[Role]
      ,[CreatedAt]
      ,[Plan]
      ,[IsActive]
      ,[LastLogin]
      ,[SubscriptionCycle]
  FROM [FacilityIADb].[dbo].[Users]
/*Este comando apaga todos os usuários e reseta o contador de IDs
TRUNCATE TABLE Users;*/



/*Dá acesso total ao sistema (incluindo dashboard administrativo, se houver).
UPDATE [Users]
SET [Role] = 'admin',
    [Plan] = 'Enterprise'
WHERE [Email] = 'Facility.ia001@gmail.com'; */



/* 1. Primeiro, remove o histórico de chat desse usuário para evitar erro
DELETE FROM [Messages] 
WHERE [UserId] = (SELECT [Id] FROM [Users] WHERE [Email] = 'klockk27@gmail.com');
-- 2. Agora sim, exclui o usuário
DELETE FROM [Users] 
WHERE [Email] = 'klockk27@gmail.com';*/

/*Testar Plano Free (Deve bloquear na 8ª mensagem)
UPDATE Users SET [Plan] = 'Free' WHERE Email = 'admin@facility.ia';

-- Testar Plano Médio (Deve bloquear na 81ª mensagem)
UPDATE Users SET [Plan] = 'Medium' WHERE Email = 'admin@facility.ia';

-- Voltar a ser patrão (Top/Enterprise)
UPDATE Users SET [Plan] = 'Top' WHERE Email = 'admin@facility.ia';*/