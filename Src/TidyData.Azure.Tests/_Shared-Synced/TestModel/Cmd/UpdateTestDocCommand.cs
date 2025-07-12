﻿#nullable disable

// ReSharper disable CheckNamespace
namespace TidyData.Tests._Shared_Synced.TestModel.Cmd;

public class UpdateTestDocCommand : ICommand<TestDataModel>
{
    private readonly TestDocument _updatedDocument;

    public UpdateTestDocCommand(TestDocument updatedDocument)
    {
        this._updatedDocument = updatedDocument;
    }

    public void Execute(TestDataModel model, CollectionWrapperFactory factory)
    {
        factory.Get(model, x => x.Docs)
            .Update(this._updatedDocument);
    }
}