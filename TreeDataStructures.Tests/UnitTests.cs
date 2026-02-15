using TreeDataStructures.Implementations.AVL;
using TreeDataStructures.Implementations.BST;
using TreeDataStructures.Implementations.RedBlackTree;
using TreeDataStructures.Implementations.Splay;
using TreeDataStructures.Implementations.Treap;
using TreeDataStructures.Tests.Base;

namespace TreeDataStructures.Tests;

[TestFixture, Category("BST")]
public class BinarySearchTreeTests : GenericTreeTests<BinarySearchTree<int, string>> { }

[TestFixture, Category("AVL")]
public class AvlTests : GenericTreeTests<AvlTree<int, string>> { }

[TestFixture, Category("RB")]
public class RedBlackTests : GenericTreeTests<RedBlackTree<int, string>> { }

[TestFixture, Category("Splay")]
public class SplayTests : GenericTreeTests<SplayTree<int, string>> { }

[TestFixture, Category("Treap")]
public class TreapTests : GenericTreeTests<Treap<int, string>> { }