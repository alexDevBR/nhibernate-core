using System;
using Antlr.Runtime;
using NHibernate.Type;

namespace NHibernate.Hql.Ast.ANTLR.Tree
{
	/// <summary>
	/// Author: Steve Ebersole
	/// Ported by: Steve Strong
	/// </summary>
	[CLSCompliant(false)]
	public class InLogicOperatorNode : BinaryLogicOperatorNode, IBinaryOperatorNode
	{
		public InLogicOperatorNode(IToken token)
			: base(token)
		{
		}

		private IASTNode InList
		{
			get { return RightHandOperand; }
		}

		public override void Initialize()
		{
			IASTNode lhs = LeftHandOperand;
			if (lhs == null)
			{
				throw new SemanticException("left-hand operand of in operator was null");
			}

			IASTNode inList = InList;
			if (inList == null)
			{
				throw new SemanticException("right-hand operand of in operator was null");
			}

			// for expected parameter type injection, we expect that the lhs represents
			// some form of property ref and that the children of the in-list represent
			// one-or-more params.
			var lhsNode = lhs as SqlNode;
			if (lhsNode != null)
			{
				IType lhsType = lhsNode.DataType;
				IASTNode inListChild = inList.GetChild(0);
				var shouldProcessMetaTypeDiscriminator = ShouldProcessMetaTypeDiscriminator(lhs);
				while (inListChild != null)
				{
					if (shouldProcessMetaTypeDiscriminator)
						ProcessMetaTypeDiscriminatorIfNecessary(lhs, inListChild);
					var expectedTypeAwareNode = inListChild as IExpectedTypeAwareNode;
					if (expectedTypeAwareNode != null)
					{
						expectedTypeAwareNode.ExpectedType = lhsType;
					}
					inListChild = inListChild.NextSibling;
				}
			}
		}

		private bool ShouldProcessMetaTypeDiscriminator(IASTNode lhs)
		{
			// This was adapted from BinaryLogicOperatorNode.ProcessMetaTypeDiscriminatorIfNecessary
			var lhsNode = lhs as SqlNode;
			if (lhsNode == null)
			{
				return false;
			}
			return lhsNode.DataType is MetaType;
		}

		private void ProcessMetaTypeDiscriminatorIfNecessary(IASTNode lhs, IASTNode rhs)
		{
			// this method inserts the discriminator value for the rhs node so that .class queries on <any> mappings work with the class name
			var lhsNode = lhs as SqlNode;
			var rhsNode = rhs as SqlNode;
			if (rhsNode == null)
			{
				return;
			}

			var lhsNodeMetaType = lhsNode.DataType as MetaType;
			string className = SessionFactoryHelper.GetImportedClassName(rhsNode.OriginalText);

			object discriminatorValue = lhsNodeMetaType.GetMetaValue(NHibernate.Util.TypeNameParser.Parse(className).Type);
			rhsNode.Text = discriminatorValue.ToString();
			return;
		}
	}
}
