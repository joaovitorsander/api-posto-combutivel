﻿using ApiPostoCombustivel.Database.Repositories;
using ApiPostoCombustivel.Database;
using ApiPostoCombustivel.Database.Repositories.Interfaces;
using ApiPostoCombustivel.DTO.AbastecimentoDTO;
using ApiPostoCombustivel.Parser;
using System.Collections.Generic;
using System.Linq;
using ApiPostoCombustivel.Validations;
using ApiPostoCombustivel.Exceptions;

namespace ApiPostoCombustivel.Services
{
    public class AbastecimentoService
    {
        private readonly AbastecimentoRepository _abastecimentoRepository;
        private readonly CombustivelRepository _combustivelRepository;

        public AbastecimentoService(AppDbContext context)
        {
            _abastecimentoRepository = new AbastecimentoRepository(context);
            _combustivelRepository = new CombustivelRepository(context);
        }

        public IEnumerable<AbastecimentoDTO> GetAbastecimentos()
        {
            var abastecimentos = _abastecimentoRepository.GetAbastecimentos();
            return abastecimentos.Select(AbastecimentoParser.ToDTO);
        }

        public AbastecimentoDTO GetAbastecimentoById(int id)
        {
            AbastecimentoValidator.ValidarAbastecimentoExistente(_abastecimentoRepository, id);
            var abastecimento = _abastecimentoRepository.GetAbastecimentoById(id);
            return AbastecimentoParser.ToDTO(abastecimento);
        }

        public IEnumerable<AbastecimentoDTO> GetAbastecimentosByTipo(string tipoCombustivel)
        {
            TipoCombustivelValidator.ValidarTipo(tipoCombustivel);
            var abastecimentos = _abastecimentoRepository.GetAbastecimentosByTipo(tipoCombustivel);
            return abastecimentos.Select(AbastecimentoParser.ToDTO);
        }

        public AbastecimentoDTO AddAbastecimento(AbastecimentoDTO abastecimentoDto)
        {
            AbastecimentoValidator.ValidarCombustivelExistente(_combustivelRepository, abastecimentoDto.TipoCombustivel);
            AbastecimentoValidator.ValidarQuantidade(abastecimentoDto.Quantidade);

            var combustivel = _combustivelRepository.GetCombustivelByTipo(abastecimentoDto.TipoCombustivel);
            AbastecimentoValidator.ValidarEstoqueSuficiente(combustivel.Estoque, abastecimentoDto.Quantidade);

            combustivel.Estoque -= abastecimentoDto.Quantidade;
            _combustivelRepository.UpdateCombustivel(combustivel);


            var abastecimento = AbastecimentoParser.ToModel(abastecimentoDto);
            _abastecimentoRepository.AddAbastecimento(abastecimento);

            return AbastecimentoParser.ToDTO(abastecimento);
        }

        //Só para deixar registrado que este método esta funcionando agora do jeito que eu quero, obrigado Deus
        public AbastecimentoDTO UpdateAbastecimento(int id, UpdateAbastecimentoDTO updateDto)
        {
            AbastecimentoValidator.ValidarAbastecimentoExistente(_abastecimentoRepository, id);
            var abastecimento = _abastecimentoRepository.GetAbastecimentoById(id);

            var combustivelOriginal = _combustivelRepository.GetCombustivelByTipo(abastecimento.TipoCombustivel);
            if (combustivelOriginal == null)
            {
                throw new CombustivelNaoEncontradoException("Combustível original não encontrado.");
            }

            bool tipoCombustivelAlterado = updateDto.TipoCombustivel != null && updateDto.TipoCombustivel != abastecimento.TipoCombustivel;

            if (tipoCombustivelAlterado)
            {
                TipoCombustivelValidator.ValidarTipo(updateDto.TipoCombustivel);
                AbastecimentoValidator.ValidarCombustivelExistente(_combustivelRepository, updateDto.TipoCombustivel);

                var novoCombustivel = _combustivelRepository.GetCombustivelByTipo(updateDto.TipoCombustivel);
                var quantidadeNecessaria = updateDto.Quantidade ?? abastecimento.Quantidade;
                EstoqueCombustivelValidator.ValidarEstoqueSuficiente(novoCombustivel.Estoque, quantidadeNecessaria);

                combustivelOriginal.Estoque += abastecimento.Quantidade;
                _combustivelRepository.UpdateCombustivel(combustivelOriginal);

                novoCombustivel.Estoque -= quantidadeNecessaria;
                _combustivelRepository.UpdateCombustivel(novoCombustivel);

                abastecimento.TipoCombustivel = updateDto.TipoCombustivel;
            }

            if (updateDto.Quantidade.HasValue)
            {
                var diferencaQuantidade = updateDto.Quantidade.Value - abastecimento.Quantidade;
                if (!tipoCombustivelAlterado)
                {
                    EstoqueCombustivelValidator.ValidarEstoqueSuficiente(combustivelOriginal.Estoque, diferencaQuantidade);
                    combustivelOriginal.Estoque -= diferencaQuantidade;
                    _combustivelRepository.UpdateCombustivel(combustivelOriginal);
                }

                abastecimento.Quantidade = updateDto.Quantidade.Value;
            }

            if (updateDto.Data.HasValue)
            {
                abastecimento.Data = updateDto.Data.Value;
            }

            _abastecimentoRepository.UpdateAbastecimento(abastecimento);

            return AbastecimentoParser.ToDTO(abastecimento);
        }

        public void DeleteAbastecimento(int id)
        {
            AbastecimentoValidator.ValidarAbastecimentoExistente(_abastecimentoRepository, id);
            _abastecimentoRepository.DeleteAbastecimento(id);
        }

        public object GetRelatorioPorDia(DateTime data)
        {
            var abastecimentosDoDia = _abastecimentoRepository.GetAbastecimentosByData(data);
            var tiposCombustiveisAbastecidos = abastecimentosDoDia
                                                .Select(a => a.TipoCombustivel)
                                                .Distinct()
                                                .ToList();

            var estoqueAtual = _combustivelRepository.GetEstoque()
                                   .Where(c => tiposCombustiveisAbastecidos.Contains(c.Tipo))
                                   .ToList();

            return new
            {
                AbastecimentosDiarios = abastecimentosDoDia,
                EstoqueAtual = estoqueAtual
            };
        }
    }
}
